using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using SecurePaste.Models;
using System.Diagnostics;

namespace SecurePaste.Services
{
    public class PresidioService : IDisposable
    {
        private readonly Configuration _config;
        private readonly IHost _host;
        private readonly IPythonEnvironment _pythonEnv;
        private bool _disposed;

        public PresidioService(Configuration config)
        {
            _config = config;

            var builder = Host.CreateApplicationBuilder();
            var pythonHome = Environment.CurrentDirectory;
            var venv = Path.Combine(pythonHome, ".venv");

            builder.Services
                .WithPython()
                .WithHome(pythonHome)
                .WithVirtualEnvironment(venv)
                .WithPipInstaller()
                .FromRedistributable();

            _host = builder.Build();
            _pythonEnv = _host.Services.GetRequiredService<IPythonEnvironment>();
        }

        public async Task<string> AnonymizeTextAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            try
            {
                var presidioConfig = new
                {
                    entities = _config.Entities.Where(e => e.Enabled).Select(e => new
                    {
                        type = e.Type,
                        anonymization_method = e.AnonymizationMethod,
                        custom_replacement = e.CustomReplacement
                    }).ToArray(),
                    confidence_threshold = _config.ConfidenceThreshold,
                    language = _config.Language
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
                _host?.Dispose();
                _disposed = true;
            }
        }
    }
}
