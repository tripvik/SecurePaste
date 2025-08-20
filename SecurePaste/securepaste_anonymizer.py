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
ANALYZER = AnalyzerEngine() if PRESIDIO_AVAILABLE else None
ANONYMIZER = AnonymizerEngine() if PRESIDIO_AVAILABLE else None

def create_operator_config(method: str, custom_replacement: Optional[str] = None):
    """Create Presidio OperatorConfig object."""
    if method == 'redact':
        return OperatorConfig('redact')
    elif method == 'replace':
        return OperatorConfig('replace', {'new_value': custom_replacement or '[REDACTED]'})
    elif method == 'mask':
        return OperatorConfig('mask', {'masking_char': '*', 'chars_to_mask': 7, 'from_end': False})
    elif method == 'hash':
        return OperatorConfig('hash')
    elif method == 'encrypt':
        return OperatorConfig('encrypt')
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
        confidence_threshold = config.get('confidence_threshold', 0.35)  # Lower threshold for better phone detection
        language = config.get('language', 'en')
        
        # Analyze text for PII entities
        analyzer_results = ANALYZER.analyze(
            text=text,
            entities=entity_types,
            language=language,
            score_threshold=confidence_threshold
        )
        
        # Build operators dictionary for ALL configured entity types (recommended approach)
        operators = {}
        entity_config_map = {e['type']: e for e in config['entities']}
        
        # Pre-define operators for all entity types in config
        for entity_config in config['entities']:
            operators[entity_config['type']] = create_operator_config(
                entity_config['anonymization_method'],
                entity_config.get('custom_replacement')
            )
        
        # Add DEFAULT operator as fallback (recommended)
        if 'DEFAULT' not in operators:
            operators['DEFAULT'] = OperatorConfig('replace', {'new_value': '[REDACTED]'})
        
        # Anonymize the text
        anonymized_result = ANONYMIZER.anonymize(
            text=text,
            analyzer_results=analyzer_results,
            operators=operators
        )
        
        # Count entities found
        entities_found = {}
        for res in analyzer_results:
            entities_found[res.entity_type] = entities_found.get(res.entity_type, 0) + 1
        
        return json.dumps({
            'success': True,
            'anonymized_text': anonymized_result.text,
            'entities_found': entities_found,
            'total_entities': len(analyzer_results),
            'analyzer_results': [
                {
                    'entity_type': res.entity_type,
                    'start': res.start,
                    'end': res.end,
                    'score': res.score,
                    'text': text[res.start:res.end]
                } for res in analyzer_results
            ]  # Added for debugging
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
