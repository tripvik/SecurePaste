using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using SecurePaste.Models;
using System.Diagnostics;

namespace SecurePaste.Services
{
    public class PresidioService : IPresidioService, IDisposable
    {
        private readonly IConfigurationService _configService;
        private readonly IPythonEnvironment _pythonEnv;
        private bool _disposed;

        public PresidioService(IConfigurationService configService, IPythonEnvironment pythonEnv)
        {
            _configService = configService;
            _pythonEnv = pythonEnv;
        }

        public async Task<string> AnonymizeTextAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            try
            {
                var config = _configService.GetConfiguration();
                var presidioConfig = new
                {
                    entities = config.Entities.Where(e => e.Enabled).Select(e => new
                    {
                        type = e.Type,
                        anonymization_method = e.AnonymizationMethod,
                        custom_replacement = e.CustomReplacement
                    }).ToArray(),
                    custom_patterns = config.CustomPatterns.Where(p => p.Enabled).Select(p => new
                    {
                        name = p.Name,
                        pattern = p.Pattern,
                        entity_type = p.EntityType,
                        enabled = p.Enabled,
                        confidence_score = p.ConfidenceScore,
                        anonymization_method = p.AnonymizationMethod,
                        custom_replacement = p.CustomReplacement,
                        description = p.Description
                    }).ToArray(),
                    confidence_threshold = config.ConfidenceThreshold,
                    language = config.Language
                };

                var configJson = JsonConvert.SerializeObject(presidioConfig);
                var module = _pythonEnv.SecurepasteAnonymizer();
                var resultJson = await Task.Run(() => module.AnonymizeText(text, configJson));

                var result = JsonConvert.DeserializeObject<AnonymizationResult>(resultJson);
                return result?.AnonymizedText ?? text;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Anonymization failed: {ex}");
                return text;
            }
        }

        public async Task<bool> CheckPresidioInstallationAsync()
        {
            try
            {
                var module = _pythonEnv.SecurepasteAnonymizer();
                var resultJson = await Task.Run(() => module.TestPresidioInstallation());

                var testResult = JsonConvert.DeserializeObject<Dictionary<string, object>>(resultJson);

                return testResult?.ContainsKey("success") == true &&
                       testResult["success"]?.ToString()?.ToLower() == "true";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Presidio check failed: {ex}");
                return false;
            }
        }

        public async Task<bool> VerifyAndUpdateInstallationStatusAsync()
        {
            try
            {
                var isInstalled = await CheckPresidioInstallationAsync();
                
                if (isInstalled)
                {
                    var config = _configService.GetConfiguration();
                    config.PresidioInstalled = true;
                    _configService.SaveConfiguration(config);
                    Debug.WriteLine("Presidio installation verified and configuration updated.");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to verify and update Presidio installation status: {ex}");
                return false;
            }
        }

        public async Task<string?> GetPythonVersionAsync()
        {
            try
            {
                var module = _pythonEnv.SecurepasteAnonymizer();
                return await Task.Run(() => module.GetPythonVersion());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get Python version: {ex}");
                return null;
            }
        }

        public async Task<string> TestPasswordRecognizerAsync()
        {
            try
            {
                var module = _pythonEnv.SecurepasteAnonymizer();
                return await Task.Run(() => module.TestPasswordRecognizer());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to test password recognizer: {ex}");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        public async Task<string> TestCustomPatternAsync(string text, CustomPatternConfiguration pattern)
        {
            try
            {
                // Create a test configuration with only this pattern
                var testConfig = new
                {
                    entities = new object[0], // No standard entities
                    custom_patterns = new[]
                    {
                        new
                        {
                            name = pattern.Name,
                            pattern = pattern.Pattern,
                            entity_type = pattern.EntityType,
                            enabled = true,
                            confidence_score = pattern.ConfidenceScore,
                            anonymization_method = pattern.AnonymizationMethod,
                            custom_replacement = pattern.CustomReplacement,
                            description = pattern.Description
                        }
                    },
                    confidence_threshold = Math.Min(pattern.ConfidenceScore - 0.1, 0.1),
                    language = "en"
                };

                var configJson = JsonConvert.SerializeObject(testConfig);
                var module = _pythonEnv.SecurepasteAnonymizer();
                return await Task.Run(() => module.AnonymizeText(text, configJson));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to test custom pattern: {ex}");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        public async Task<string> ValidateCustomPatternAsync(CustomPatternConfiguration pattern)
        {
            try
            {
                var patternJson = JsonConvert.SerializeObject(new
                {
                    name = pattern.Name,
                    pattern = pattern.Pattern,
                    entity_type = pattern.EntityType,
                    confidence_score = pattern.ConfidenceScore,
                    anonymization_method = pattern.AnonymizationMethod,
                    custom_replacement = pattern.CustomReplacement,
                    description = pattern.Description
                });

                var module = _pythonEnv.SecurepasteAnonymizer();
                return await Task.Run(() => module.ValidateCustomPattern(patternJson));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to validate custom pattern: {ex}");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        private class AnonymizationResult
        {
            [JsonProperty("success")]
            public bool Success { get; set; }
            [JsonProperty("anonymized_text")]
            public string AnonymizedText { get; set; } = string.Empty;
            [JsonProperty("entities_found")]
            public Dictionary<string, int> EntitiesFound { get; set; } = new();
            [JsonProperty("total_entities")]
            public int TotalEntities { get; set; }
            [JsonProperty("error")]
            public string? Error { get; set; }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
