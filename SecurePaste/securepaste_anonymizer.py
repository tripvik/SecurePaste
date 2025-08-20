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

class CustomPatternRecognizer(PatternRecognizer):
    """
    Dynamic PatternRecognizer that can be configured with custom regex patterns
    """
    
    def __init__(self, name: str, patterns: List[Pattern], entity_type: str, context: List[str] = None):
        if not patterns:
            raise ValueError("At least one pattern must be provided")
        
        super().__init__(
            supported_entity=entity_type,
            patterns=patterns,
            context=context or [],
            supported_language="en"
        )
        self.name = name

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

def create_custom_recognizers(custom_patterns_config: List[Dict[str, Any]]) -> List[CustomPatternRecognizer]:
    """
    Creates custom pattern recognizers from configuration
    """
    recognizers = []
    
    for pattern_config in custom_patterns_config:
        if not pattern_config.get('enabled', True):
            continue
            
        try:
            # Validate the regex pattern
            import re
            re.compile(pattern_config['pattern'])
            
            pattern = Pattern(
                name=pattern_config['name'],
                regex=pattern_config['pattern'],
                score=pattern_config.get('confidence_score', 0.8)
            )
            
            recognizer = CustomPatternRecognizer(
                name=pattern_config['name'],
                patterns=[pattern],
                entity_type=pattern_config['entity_type'],
                context=[]  # Custom patterns can define their own context if needed
            )
            
            recognizers.append(recognizer)
            
        except Exception as e:
            print(f"Error creating custom recognizer '{pattern_config.get('name', 'unknown')}': {e}")
            continue
    
    return recognizers

def setup_analyzer_with_custom_patterns(custom_patterns_config: List[Dict[str, Any]] = None) -> AnalyzerEngine:
    """
    Creates an AnalyzerEngine with custom patterns and the password recognizer added.
    Returns the configured analyzer engine.
    """
    if not PRESIDIO_AVAILABLE:
        return None
    
    # Create the standard analyzer
    analyzer = AnalyzerEngine()
    
    # Add the custom password recognizer
    password_recognizer = PasswordPatternRecognizer()
    analyzer.registry.add_recognizer(password_recognizer)
    
    # Add custom pattern recognizers if provided
    if custom_patterns_config:
        custom_recognizers = create_custom_recognizers(custom_patterns_config)
        for recognizer in custom_recognizers:
            analyzer.registry.add_recognizer(recognizer)
    
    return analyzer

# Initialize engines once (better performance) - now with custom password recognizer
ANALYZER = setup_analyzer_with_custom_patterns() if PRESIDIO_AVAILABLE else None
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
        confidence_threshold = config.get('confidence_threshold', 0.35)
        language = config.get('language', 'en')
        custom_patterns_config = config.get('custom_patterns', [])
        
        # Create analyzer with custom patterns for this request
        analyzer = setup_analyzer_with_custom_patterns(custom_patterns_config)
        if not analyzer:
            raise Exception("Failed to create analyzer")
        
        # Collect all entity types (standard + custom)
        all_entity_types = entity_types.copy()
        for pattern_config in custom_patterns_config:
            if pattern_config.get('enabled', True):
                entity_type = pattern_config['entity_type']
                if entity_type not in all_entity_types:
                    all_entity_types.append(entity_type)
        
        # Analyze text for PII entities
        analyzer_results = analyzer.analyze(
            text=text,
            entities=all_entity_types,
            language=language,
            score_threshold=confidence_threshold
        )
        
        # Build operators dictionary for ALL configured entity types
        operators = {}
        entity_config_map = {e['type']: e for e in config['entities']}
        
        # Pre-define operators for standard entity types
        for entity_config in config['entities']:
            operators[entity_config['type']] = create_operator_config(
                entity_config['anonymization_method'],
                entity_config.get('custom_replacement')
            )
        
        # Add operators for custom patterns
        for pattern_config in custom_patterns_config:
            if pattern_config.get('enabled', True):
                entity_type = pattern_config['entity_type']
                operators[entity_type] = create_operator_config(
                    pattern_config.get('anonymization_method', 'redact'),
                    pattern_config.get('custom_replacement')
                )
        
        # Add DEFAULT operator as fallback
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
            ]
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

def test_custom_pattern(pattern_config_json: str, test_text: str) -> str:
    """Test function to verify a custom pattern works correctly."""
    if not PRESIDIO_AVAILABLE:
        return json.dumps({'success': False, 'error': PRESIDIO_ERROR})
    
    try:
        pattern_config = json.loads(pattern_config_json)
        
        # Create a temporary analyzer with just this pattern
        analyzer = AnalyzerEngine()
        
        # Add the password recognizer for completeness
        password_recognizer = PasswordPatternRecognizer()
        analyzer.registry.add_recognizer(password_recognizer)
        
        # Add the custom pattern
        try:
            pattern = Pattern(
                name=pattern_config['name'],
                regex=pattern_config['pattern'],
                score=pattern_config.get('confidence_score', 0.8)
            )
            
            recognizer = CustomPatternRecognizer(
                name=pattern_config['name'],
                patterns=[pattern],
                entity_type=pattern_config['entity_type']
            )
            
            analyzer.registry.add_recognizer(recognizer)
            
            # Test the pattern
            analyzer_results = analyzer.analyze(
                text=test_text,
                entities=[pattern_config['entity_type']],
                language="en",
                score_threshold=0.1  # Low threshold for testing
            )
            
            return json.dumps({
                'success': True,
                'pattern_name': pattern_config['name'],
                'entity_type': pattern_config['entity_type'],
                'test_text': test_text,
                'matches_found': len(analyzer_results),
                'matches': [
                    {
                        'text': test_text[res.start:res.end],
                        'start': res.start,
                        'end': res.end,
                        'score': res.score
                    } for res in analyzer_results
                ]
            })
            
        except Exception as pattern_error:
            return json.dumps({
                'success': False,
                'error': f'Pattern error: {str(pattern_error)}',
                'pattern_name': pattern_config.get('name', 'unknown')
            })
        
    except Exception as e:
        return json.dumps({'success': False, 'error': str(e)})

def validate_custom_pattern(pattern_json: str) -> str:
    """Validate a custom pattern without executing it."""
    try:
        pattern_config = json.loads(pattern_json)
        
        # Check required fields
        required_fields = ['name', 'pattern', 'entity_type']
        for field in required_fields:
            if not pattern_config.get(field):
                return json.dumps({'success': False, 'error': f'Missing required field: {field}'})
        
        # Validate regex pattern
        import re
        try:
            re.compile(pattern_config['pattern'])
        except re.error as e:
            return json.dumps({'success': False, 'error': f'Invalid regex pattern: {str(e)}'})
        
        # Validate confidence score
        confidence = pattern_config.get('confidence_score', 0.8)
        if not isinstance(confidence, (int, float)) or confidence < 0.1 or confidence > 1.0:
            return json.dumps({'success': False, 'error': 'Confidence score must be between 0.1 and 1.0'})
        
        # Validate anonymization method
        valid_methods = ['redact', 'replace', 'mask', 'hash', 'encrypt']
        method = pattern_config.get('anonymization_method', 'redact')
        if method not in valid_methods:
            return json.dumps({'success': False, 'error': f'Invalid anonymization method. Must be one of: {", ".join(valid_methods)}'})
        
        return json.dumps({'success': True, 'message': 'Pattern validation passed'})
        
    except json.JSONDecodeError as e:
        return json.dumps({'success': False, 'error': f'Invalid JSON: {str(e)}'})
    except Exception as e:
        return json.dumps({'success': False, 'error': str(e)})

def get_python_version() -> str:
    return f"Python {sys.version}"
