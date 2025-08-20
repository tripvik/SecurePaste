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
