import sys
import json
from typing import Optional, Dict, Any

PRESIDIO_AVAILABLE = True
PRESIDIO_ERROR = None

try:
    from presidio_analyzer import AnalyzerEngine
    from presidio_anonymizer import AnonymizerEngine
    from presidio_anonymizer.entities import OperatorConfig
except ImportError as e:
    PRESIDIO_AVAILABLE = False
    PRESIDIO_ERROR = str(e)

# Initialize engines once (better performance)
_ANALYZER = AnalyzerEngine() if PRESIDIO_AVAILABLE else None
_ANONYMIZER = AnonymizerEngine() if PRESIDIO_AVAILABLE else None


def _create_operator_config(method: str, custom_replacement: Optional[str] = None):
    """Create Presidio OperatorConfig object."""
    if method == 'redact':
        return OperatorConfig('redact')
    elif method == 'replace':
        return OperatorConfig('replace', {'new_value': custom_replacement or '[REDACTED]'})
    elif method == 'mask':
        return OperatorConfig('mask', {'masking_char': '*', 'chars_to_mask': 4, 'from_end': True})
    elif method == 'hash':
        return OperatorConfig('hash')
    return OperatorConfig('redact')


def anonymize_text(text: str, config_json: str) -> str:
    """Main function called from C#."""
    if not PRESIDIO_AVAILABLE:
        return json.dumps({
            'success': False,
            'error': f'Presidio not available: {PRESIDIO_ERROR}',
            'anonymized_text': text
        })

    try:
        config = json.loads(config_json)
        if 'entities' not in config or not isinstance(config['entities'], list):
            raise ValueError("Invalid config: missing 'entities' list.")

        entity_types = [e['type'] for e in config['entities']]
        confidence_threshold = config.get('confidence_threshold', 0.7)
        language = config.get('language', 'en')

        analyzer_results = _ANALYZER.analyze(
            text=text,
            entities=entity_types,
            language=language,
            score_threshold=confidence_threshold
        )

        operators = {}
        for result in analyzer_results:
            for entity in config['entities']:
                if entity['type'] == result.entity_type:
                    operators[result.entity_type] = _create_operator_config(
                        entity['anonymization_method'],
                        entity.get('custom_replacement')
                    )

        anonymized_result = _ANONYMIZER.anonymize(
            text=text,
            analyzer_results=analyzer_results,
            operators=operators
        )

        entities_found = {}
        for res in analyzer_results:
            entities_found[res.entity_type] = entities_found.get(res.entity_type, 0) + 1

        return json.dumps({
            'success': True,
            'anonymized_text': anonymized_result.text,
            'entities_found': entities_found,
            'total_entities': len(analyzer_results)
        })

    except Exception as e:
        return json.dumps({
            'success': False,
            'error': str(e),
            'anonymized_text': text
        })


def test_presidio_installation() -> str:
    if not PRESIDIO_AVAILABLE:
        return json.dumps({'success': False, 'error': PRESIDIO_ERROR})
    try:
        _ = AnalyzerEngine()
        _ = AnonymizerEngine()
        return json.dumps({'success': True, 'message': 'Presidio is working correctly'})
    except Exception as e:
        return json.dumps({'success': False, 'error': str(e)})


def get_python_version() -> str:
    return f"Python {sys.version}"
