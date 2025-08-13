using Newtonsoft.Json;
using SecurePaste.Models;
using System.Diagnostics;
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

        public PresidioService(Configuration config)
        {
            _config = config;
            _tempDirectory = Path.Combine(Path.GetTempPath(), "SecurePaste");
            Directory.CreateDirectory(_tempDirectory);
            _pythonScriptPath = Path.Combine(_tempDirectory, "presidio_anonymizer.py");
            
            GeneratePythonScript();
        }

        /// <summary>
        /// Anonymizes text using Presidio
        /// </summary>
        /// <param name="text">Text to anonymize</param>
        /// <returns>Anonymized text or original text if failed</returns>
        public async Task<string> AnonymizeTextAsync(string text)
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

                // Run Python script
                var processInfo = new ProcessStartInfo
                {
                    FileName = _config.PythonPath,
                    Arguments = $"\"{_pythonScriptPath}\" \"{inputFile}\" \"{outputFile}\" \"{configFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();

                var output = await outputTask;
                var error = await errorTask;

                // Clean up input and config files
                try
                {
                    File.Delete(inputFile);
                    File.Delete(configFile);
                }
                catch { }

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Python script failed with exit code {process.ExitCode}: {error}");
                }

                // Read result
                if (File.Exists(outputFile))
                {
                    try
                    {
                        var resultJson = await File.ReadAllTextAsync(outputFile);
                        var result = JsonConvert.DeserializeObject<AnonymizationResult>(resultJson);
                        
                        File.Delete(outputFile);
                        
                        return result?.AnonymizedText ?? text;
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
                var testScript = Path.Combine(_tempDirectory, "test_presidio.py");
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

                var processInfo = new ProcessStartInfo
                {
                    FileName = _config.PythonPath,
                    Arguments = $"\"{testScript}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                try { File.Delete(testScript); } catch { }

                return process.ExitCode == 0 && output.Trim() == "SUCCESS";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates the Python script for Presidio integration
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
    if len(sys.argv) != 4:
        print('Usage: python presidio_anonymizer.py <input_file> <output_file> <config_file>', file=sys.stderr)
        sys.exit(1)
    
    input_file = sys.argv[1]
    output_file = sys.argv[2]
    config_file = sys.argv[3]
    
    try:
        # Read input text
        with open(input_file, 'r', encoding='utf-8') as f:
            text = f.read()
        
        # Read configuration
        with open(config_file, 'r', encoding='utf-8') as f:
            config = json.load(f)
        
        # Anonymize text
        result = anonymize_text(text, config)
        
        # Write result
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(result, f, ensure_ascii=False, indent=2)
        
        sys.exit(0)
        
    except Exception as e:
        error_result = {
            'success': False,
            'error': str(e),
            'anonymized_text': text if 'text' in locals() else ''
        }
        
        try:
            with open(output_file, 'w', encoding='utf-8') as f:
                json.dump(error_result, f, ensure_ascii=False, indent=2)
        except:
            pass
        
        print(f'Error: {e}', file=sys.stderr)
        sys.exit(1)

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