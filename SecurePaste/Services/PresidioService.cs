using Newtonsoft.Json;
using SecurePaste.Models;
using System.Text;

namespace SecurePaste.Services
{
    /// <summary>
    /// Service for integrating with Python Presidio library
    /// </summary>
    public class PresidioService
    {
        private readonly Configuration _config;
        private readonly string _pythonScriptPath;
        private readonly string _tempDirectory;
        private readonly CliWrapper _cliWrapper;
        private readonly bool _useStdinMode;

        public PresidioService(Configuration config, bool useStdinMode = true)
        {
            _config = config;
            _tempDirectory = Path.Combine(Path.GetTempPath(), "SecurePaste");
            Directory.CreateDirectory(_tempDirectory);
            // FIXED: Changed script name to avoid conflict with presidio_anonymizer package
            _pythonScriptPath = Path.Combine(_tempDirectory, "securepaste_anonymizer.py");
            _cliWrapper = new CliWrapper();
            _useStdinMode = useStdinMode;
            
            GeneratePythonScript();
        }

        /// <summary>
        /// Anonymizes text using Presidio (uses stdin/stdout by default, falls back to files if needed)
        /// </summary>
        /// <param name="text">Text to anonymize</param>
        /// <returns>Anonymized text or original text if failed</returns>
        public async Task<string> AnonymizeTextAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Try stdin mode first (faster, no temp files)
            if (_useStdinMode)
            {
                try
                {
                    return await AnonymizeTextViaStdinAsync(text);
                }
                catch (Exception ex)
                {
                    // Log the error but fall back to file mode
                    System.Diagnostics.Debug.WriteLine($"Stdin mode failed, falling back to file mode: {ex.Message}");
                }
            }

            // Fall back to file mode if stdin fails or is disabled
            return await AnonymizeTextViaFilesAsync(text);
        }

        /// <summary>
        /// Anonymizes text using Presidio via stdin/stdout (no temp files)
        /// </summary>
        /// <param name="text">Text to anonymize</param>
        /// <returns>Anonymized text or original text if failed</returns>
        public async Task<string> AnonymizeTextViaStdinAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            try
            {
                // Create configuration with input text embedded
                var presidioConfig = new
                {
                    entities = _config.Entities.Where(e => e.Enabled).Select(e => new
                    {
                        type = e.Type,
                        anonymization_method = e.AnonymizationMethod,
                        custom_replacement = e.CustomReplacement
                    }),
                    confidence_threshold = _config.ConfidenceThreshold,
                    language = _config.Language,
                    input_text = text
                };

                var configJson = JsonConvert.SerializeObject(presidioConfig, Formatting.None);

                // Execute Python script with JSON input via stdin
                var options = new CliOptions
                {
                    Timeout = TimeSpan.FromMinutes(2),
                    CreateNoWindow = true,
                    OutputEncoding = Encoding.UTF8,
                    ErrorEncoding = Encoding.UTF8
                };

                var result = await CliWrapper.ExecutePythonWithStdinAsync(_config.PythonPath, _pythonScriptPath, configJson, "--stdin", options);

                if (!result.Success)
                {
                    throw new Exception($"Python script failed with exit code {result.ExitCode}: {result.StandardError}");
                }

                // Parse result from stdout
                var anonymizationResult = JsonConvert.DeserializeObject<AnonymizationResult>(result.StandardOutput);
                return anonymizationResult?.AnonymizedText ?? text;
            }
            catch
            {
                // Return original text if anonymization fails
                return text;
            }
        }

        /// <summary>
        /// Anonymizes text using Presidio via temporary files (fallback method)
        /// </summary>
        /// <param name="text">Text to anonymize</param>
        /// <returns>Anonymized text or original text if failed</returns>
        public async Task<string> AnonymizeTextViaFilesAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            try
            {
                // Create input file
                var inputFile = Path.Combine(_tempDirectory, $"input_{Guid.NewGuid()}.txt");
                var outputFile = Path.Combine(_tempDirectory, $"output_{Guid.NewGuid()}.json");
                var configFile = Path.Combine(_tempDirectory, $"config_{Guid.NewGuid()}.json");

                // Write input text
                await File.WriteAllTextAsync(inputFile, text, Encoding.UTF8);

                // Write configuration
                var presidioConfig = new
                {
                    entities = _config.Entities.Where(e => e.Enabled).Select(e => new
                    {
                        type = e.Type,
                        anonymization_method = e.AnonymizationMethod,
                        custom_replacement = e.CustomReplacement
                    }),
                    confidence_threshold = _config.ConfidenceThreshold,
                    language = _config.Language
                };

                await File.WriteAllTextAsync(configFile, JsonConvert.SerializeObject(presidioConfig, Formatting.Indented));

                // Execute Python script using CLI wrapper
                var scriptArguments = $"\"{inputFile}\" \"{outputFile}\" \"{configFile}\"";
                var options = new CliOptions
                {
                    Timeout = TimeSpan.FromMinutes(2),
                    CreateNoWindow = true,
                    OutputEncoding = Encoding.UTF8,
                    ErrorEncoding = Encoding.UTF8
                };

                var result = await CliWrapper.ExecutePythonAsync(_config.PythonPath, _pythonScriptPath, scriptArguments, options);

                // Clean up input and config files
                try
                {
                    File.Delete(inputFile);
                    File.Delete(configFile);
                }
                catch { }

                if (!result.Success)
                {
                    throw new Exception($"Python script failed with exit code {result.ExitCode}: {result.StandardError}");
                }

                // Read result
                if (File.Exists(outputFile))
                {
                    try
                    {
                        var resultJson = await File.ReadAllTextAsync(outputFile);
                        var anonymizationResult = JsonConvert.DeserializeObject<AnonymizationResult>(resultJson);
                        
                        File.Delete(outputFile);
                        
                        return anonymizationResult?.AnonymizedText ?? text;
                    }
                    catch
                    {
                        try { File.Delete(outputFile); } catch { }
                        return text;
                    }
                }

                return text;
            }
            catch
            {
                // Return original text if anonymization fails
                return text;
            }
        }

        /// <summary>
        /// Checks if Presidio is installed and working
        /// </summary>
        /// <returns>True if Presidio is available</returns>
        public async Task<bool> CheckPresidioInstallationAsync()
        {
            try
            {
                // Use a different script name to avoid conflicts
                var testScript = Path.Combine(_tempDirectory, "test_presidio_check.py");
                await File.WriteAllTextAsync(testScript, @"
import sys
try:
    from presidio_analyzer import AnalyzerEngine
    from presidio_anonymizer import AnonymizerEngine
    print('SUCCESS')
    sys.exit(0)
except ImportError as e:
    print(f'IMPORT_ERROR: {e}')
    sys.exit(1)
except Exception as e:
    print(f'ERROR: {e}')
    sys.exit(1)
");

                var options = new CliOptions
                {
                    Timeout = TimeSpan.FromSeconds(30),
                    CreateNoWindow = true
                };

                var result = await CliWrapper.ExecutePythonAsync(_config.PythonPath, testScript, "", options);

                try { File.Delete(testScript); } catch { }

                return result.Success && result.StandardOutput.Trim() == "SUCCESS";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tests if Python is available and working
        /// </summary>
        /// <returns>True if Python is available</returns>
        public async Task<bool> TestPythonInstallationAsync()
        {
            try
            {
                return await CliWrapper.TestCommandExistsAsync(_config.PythonPath, "--version");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets Python version information
        /// </summary>
        /// <returns>Python version string or null if failed</returns>
        public async Task<string?> GetPythonVersionAsync()
        {
            try
            {
                var options = new CliOptions
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };

                var result = await CliWrapper.ExecuteAsync(_config.PythonPath, "--version", options);
                
                if (result.Success)
                {
                    return result.StandardOutput.Trim();
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Installs Python packages using pip
        /// </summary>
        /// <param name="packages">Package names to install</param>
        /// <returns>Installation result</returns>
        public async Task<CliResult> InstallPythonPackagesAsync(params string[] packages)
        {
            if (packages == null || packages.Length == 0)
                throw new ArgumentException("At least one package must be specified", nameof(packages));

            var packageList = string.Join(" ", packages);
            var pipArguments = $"-m pip install --upgrade {packageList}";
            
            var options = new CliOptions
            {
                Timeout = TimeSpan.FromMinutes(10), // Longer timeout for installations
                CreateNoWindow = true
            };

            return await CliWrapper.ExecuteAsync(_config.PythonPath, pipArguments, options);
        }

        /// <summary>
        /// Tests stdin/stdout functionality
        /// </summary>
        /// <returns>True if stdin/stdout mode is working</returns>
        public async Task<bool> TestStdinModeAsync()
        {
            try
            {
                var testText = "Hello John Smith, your email is test@example.com";
                var result = await AnonymizeTextViaStdinAsync(testText);
                return !string.Equals(testText, result, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates the Python script for Presidio integration with both stdin and file support
        /// </summary>
        private void GeneratePythonScript()
        {
            var script = @"
import sys
import json
import argparse
from typing import List, Dict, Any

try:
    from presidio_analyzer import AnalyzerEngine
    from presidio_anonymizer import AnonymizerEngine
    from presidio_anonymizer.entities import OperatorConfig
except ImportError as e:
    # Handle import error differently based on mode
    if '--stdin' in sys.argv:
        error_result = {'success': False, 'error': f'Error importing Presidio: {e}', 'anonymized_text': ''}
        print(json.dumps(error_result, ensure_ascii=False), file=sys.stdout)
    else:
        print(f'Error importing Presidio: {e}', file=sys.stderr)
    sys.exit(1)

def create_operator_config(method: str, custom_replacement: str = None) -> OperatorConfig:
    """"""Create operator configuration based on method""""""
    if method == 'redact':
        return OperatorConfig('redact')
    elif method == 'replace':
        if custom_replacement:
            return OperatorConfig('replace', {'new_value': custom_replacement})
        return OperatorConfig('replace', {'new_value': '[REDACTED]'})
    elif method == 'mask':
        return OperatorConfig('mask', {'masking_char': '*', 'chars_to_mask': 4, 'from_end': True})
    elif method == 'hash':
        return OperatorConfig('hash')
    else:
        return OperatorConfig('redact')

def anonymize_text(text: str, config: Dict[str, Any]) -> Dict[str, Any]:
    """"""Anonymize text using Presidio""""""
    try:
        # Initialize engines
        analyzer = AnalyzerEngine()
        anonymizer = AnonymizerEngine()
        
        # Get enabled entities
        entity_types = [entity['type'] for entity in config['entities']]
        confidence_threshold = config.get('confidence_threshold', 0.7)
        language = config.get('language', 'en')
        
        # Analyze text
        analyzer_results = analyzer.analyze(
            text=text,
            entities=entity_types,
            language=language,
            score_threshold=confidence_threshold
        )
        
        # Create operators configuration
        operators = {}
        entity_config_map = {entity['type']: entity for entity in config['entities']}
        
        for result in analyzer_results:
            entity_type = result.entity_type
            if entity_type in entity_config_map:
                entity_cfg = entity_config_map[entity_type]
                operators[entity_type] = create_operator_config(
                    entity_cfg['anonymization_method'],
                    entity_cfg.get('custom_replacement')
                )
        
        # Anonymize text
        anonymized_result = anonymizer.anonymize(
            text=text,
            analyzer_results=analyzer_results,
            operators=operators
        )
        
        # Prepare result
        entities_found = {}
        for result in analyzer_results:
            entity_type = result.entity_type
            entities_found[entity_type] = entities_found.get(entity_type, 0) + 1
        
        return {
            'success': True,
            'anonymized_text': anonymized_result.text,
            'entities_found': entities_found,
            'total_entities': len(analyzer_results)
        }
        
    except Exception as e:
        return {
            'success': False,
            'error': str(e),
            'anonymized_text': text
        }

def main():
    try:
        # Check if using stdin mode
        if '--stdin' in sys.argv:
            # Read JSON input from stdin
            input_data = sys.stdin.read()
            if not input_data.strip():
                error_result = {'success': False, 'error': 'No input data received from stdin', 'anonymized_text': ''}
                print(json.dumps(error_result, ensure_ascii=False), file=sys.stdout)
                sys.exit(1)
            
            config = json.loads(input_data)
            text = config.get('input_text', '')
            
            if not text:
                error_result = {'success': False, 'error': 'No input_text found in configuration', 'anonymized_text': ''}
                print(json.dumps(error_result, ensure_ascii=False), file=sys.stdout)
                sys.exit(1)
        else:
            # File mode (backward compatibility)
            if len(sys.argv) != 4:
                if '--stdin' not in sys.argv:
                    print('Usage: python securepaste_anonymizer.py <input_file> <output_file> <config_file>', file=sys.stderr)
                    print('   or: python securepaste_anonymizer.py --stdin', file=sys.stderr)
                sys.exit(1)
            
            input_file = sys.argv[1];
            output_file = sys.argv[2];
            config_file = sys.argv[3];
            
            # Read input text
            with open(input_file, 'r', encoding='utf-8') as f:
                text = f.read()
            
            # Read configuration
            with open(config_file, 'r', encoding='utf-8') as f:
                config = json.load(f)
        
        # Anonymize text
        result = anonymize_text(text, config)
        
        if '--stdin' in sys.argv:
            # Output JSON to stdout
            print(json.dumps(result, ensure_ascii=False), file=sys.stdout)
        else:
            # Write result to file (backward compatibility)
            with open(output_file, 'w', encoding='utf-8') as f:
                json.dump(result, f, ensure_ascii=False, indent=2)
        
        sys.exit(0)
        
    except json.JSONDecodeError as e:
        error_result = {
            'success': False,
            'error': f'JSON decode error: {e}',
            'anonymized_text': ''
        }
        
        if '--stdin' in sys.argv:
            print(json.dumps(error_result, ensure_ascii=False), file=sys.stdout)
        else:
            print(f'JSON decode error: {e}', file=sys.stderr)
        sys.exit(1);
        
    except FileNotFoundError as e:
        error_result = {
            'success': False,
            'error': f'File not found: {e}',
            'anonymized_text': ''
        };
        
        if '--stdin' in sys.argv:
            print(json.dumps(error_result, ensure_ascii=False), file=sys.stdout)
        else:
            print(f'File not found: {e}', file=sys.stderr)
            # Try to write error to output file if in file mode
            if 'output_file' in locals():
                try:
                    with open(output_file, 'w', encoding='utf-8') as f:
                        json.dump(error_result, f, ensure_ascii=False, indent=2)
                except:
                    pass
        sys.exit(1);
        
    except Exception as e:
        error_result = {
            'success': False,
            'error': str(e),
            'anonymized_text': ''
        };
        
        if '--stdin' in sys.argv:
            print(json.dumps(error_result, ensure_ascii=False), file=sys.stdout)
        else:
            print(f'Error: {e}', file=sys.stderr)
            # Try to write error to output file if in file mode
            if 'output_file' in locals():
                try:
                    with open(output_file, 'w', encoding='utf-8') as f:
                        json.dump(error_result, f, ensure_ascii=False, indent=2)
                except:
                    pass
        sys.exit(1);

if __name__ == '__main__':
    main()
";
            File.WriteAllText(_pythonScriptPath, script);
        }

        /// <summary>
        /// Result from anonymization operation
        /// </summary>
        private class AnonymizationResult
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("anonymized_text")]
            public string AnonymizedText { get; set; } = string.Empty;

            [JsonProperty("entities_found")]
            public Dictionary<string, int> EntitiesFound { get; set; } = new Dictionary<string, int>();

            [JsonProperty("total_entities")]
            public int TotalEntities { get; set; }

            [JsonProperty("error")]
            public string? Error { get; set; }
        }
    }
}