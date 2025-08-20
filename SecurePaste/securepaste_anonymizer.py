import sys
import json
import re
from typing import Optional, Dict, Any, List

PRESIDIO_AVAILABLE = True
PRESIDIO_ERROR = None
try:
    from presidio_analyzer import AnalyzerEngine, Pattern, PatternRecognizer
    from presidio_anonymizer import AnonymizerEngine
    from presidio_anonymizer.entities import OperatorConfig
except ImportError as e:
    PRESIDIO_AVAILABLE = False
    PRESIDIO_ERROR = str(e)

class PasswordPatternRecognizer(PatternRecognizer):
    """
    Custom PatternRecognizer for detecting passwords in various formats.
    Detects passwords in contexts like:
    - password: value, pwd: value, pass: value
    - password = value, pwd = value
    - password is value
    - Quoted passwords: password="value" or password='value'
    - Login credential pairs: username=user password=pass
    """
    
    PATTERNS = [
        # Pattern 1: password: value, pwd: value, pass: value (case insensitive)
        Pattern(
            name="password_colon_pattern",
            regex=r"(?i)(?:password|pwd|pass)\s*:\s*([^\s\'\"]{6,})",
            score=0.8
        ),
        # Pattern 2: password = value, pwd = value (case insensitive)
        Pattern(
            name="password_equals_pattern", 
            regex=r"(?i)(?:password|pwd|pass)\s*=\s*([^\s\'\"]{6,})",
            score=0.8
        ),
        # Pattern 3: password is value
        Pattern(
            name="password_is_pattern",
            regex=r"(?i)(?:password|pwd|pass)\s+is\s+([^\s\'\"]{6,})",
            score=0.7
        ),
        # Pattern 4: Quoted passwords - double quotes
        Pattern(
            name="password_quoted_double",
            regex=r"(?i)(?:password|pwd|pass)\s*[:=]\s*\"([^\"]{6,})\"",
            score=0.9
        ),
        # Pattern 5: Quoted passwords - single quotes
        Pattern(
            name="password_quoted_single",
            regex=r"(?i)(?:password|pwd|pass)\s*[:=]\s*'([^']{6,})'",
            score=0.9
        ),
        # Pattern 6: Login credential pairs - username=user password=pass
        Pattern(
            name="login_credentials_pattern",
            regex=r"(?i)(?:username|user|login)\s*[:=]\s*\S+\s+(?:password|pwd|pass)\s*[:=]\s*([^\s\'\"]{6,})",
            score=0.85
        )
    ]
    
    CONTEXT = [
        "password", "pwd", "pass", "credential", "auth", "login", 
        "signin", "authentication", "secret", "key"
    ]
    
    def __init__(self):
        super().__init__(
            supported_entity="PASSWORD",
            patterns=self.PATTERNS,
            context=self.CONTEXT,
            supported_language="en"
        )

def setup_analyzer_with_password_recognizer() -> AnalyzerEngine:
    """
    Creates an AnalyzerEngine with the custom password recognizer added.
    Returns the configured analyzer engine.
    """
    if not PRESIDIO_AVAILABLE:
        return None
    
    # Create the standard analyzer
    analyzer = AnalyzerEngine()
    
    # Create and add the custom password recognizer
    password_recognizer = PasswordPatternRecognizer()
    analyzer.registry.add_recognizer(password_recognizer)
    
    return analyzer

# Initialize engines once (better performance) - now with custom password recognizer
ANALYZER = setup_analyzer_with_password_recognizer() if PRESIDIO_AVAILABLE else None
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

def test_password_recognizer() -> str:
    """Test function to verify the custom password recognizer is working correctly."""
    if not PRESIDIO_AVAILABLE:
        return json.dumps({'success': False, 'error': PRESIDIO_ERROR})
    
    try:
        # Test cases for different password patterns
        test_cases = [
            "password: mySecretPass123",
            "pwd: anotherPass456", 
            "password = testPassword789",
            "password is myPassword123",
            'password="quotedPassword123"',
            "password='singleQuoted456'",
            "username=john password=secret123456"
        ]
        
        results = []
        for test_text in test_cases:
            analyzer_results = ANALYZER.analyze(
                text=test_text,
                entities=["PASSWORD"],
                language="en",
                score_threshold=0.5
            )
            
            results.append({
                'text': test_text,
                'found_passwords': len(analyzer_results),
                'entities': [
                    {
                        'entity_type': res.entity_type,
                        'start': res.start,
                        'end': res.end,
                        'score': res.score,
                        'detected_text': test_text[res.start:res.end]
                    } for res in analyzer_results
                ]
            })
        
        return json.dumps({
            'success': True, 
            'message': 'Password recognizer test completed',
            'test_results': results
        }, indent=2)
        
    except Exception as e:
        return json.dumps({'success': False, 'error': str(e)})

def validate_regex_patterns() -> str:
    """Validate that all regex patterns compile correctly."""
    try:
        import re
        patterns = [
            r"(?i)(?:password|pwd|pass)\s*:\s*([^\s\'\"]{6,})",
            r"(?i)(?:password|pwd|pass)\s*=\s*([^\s\'\"]{6,})",
            r"(?i)(?:password|pwd|pass)\s+is\s+([^\s\'\"]{6,})",
            r"(?i)(?:password|pwd|pass)\s*[:=]\s*\"([^\"]{6,})\"",
            r"(?i)(?:password|pwd|pass)\s*[:=]\s*'([^']{6,})'",
            r"(?i)(?:username|user|login)\s*[:=]\s*\S+\s+(?:password|pwd|pass)\s*[:=]\s*([^\s\'\"]{6,})"
        ]
        
        for i, pattern in enumerate(patterns):
            re.compile(pattern)
        
        return json.dumps({'success': True, 'message': 'All regex patterns are valid'})
    except Exception as e:
        return json.dumps({'success': False, 'error': f'Pattern validation failed: {str(e)}'})

def get_python_version() -> str:
    return f"Python {sys.version}"
