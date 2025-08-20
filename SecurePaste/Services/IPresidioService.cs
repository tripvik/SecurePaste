using SecurePaste.Models;

namespace SecurePaste.Services
{
    /// <summary>
    /// Interface for Presidio anonymization service
    /// </summary>
    public interface IPresidioService : IDisposable
    {
        /// <summary>
        /// Anonymizes text using Presidio
        /// </summary>
        /// <param name="text">Text to anonymize</param>
        /// <returns>Anonymized text</returns>
        Task<string> AnonymizeTextAsync(string text);

        /// <summary>
        /// Checks if Presidio is properly installed and working
        /// </summary>
        /// <returns>True if Presidio is working, false otherwise</returns>
        Task<bool> CheckPresidioInstallationAsync();

        /// <summary>
        /// Gets the Python version
        /// </summary>
        /// <returns>Python version string</returns>
        Task<string?> GetPythonVersionAsync();

        /// <summary>
        /// Verifies Presidio installation and updates configuration if successful
        /// </summary>
        /// <returns>True if verification was successful and configuration updated</returns>
        Task<bool> VerifyAndUpdateInstallationStatusAsync();

        /// <summary>
        /// Tests the custom password recognizer functionality
        /// </summary>
        /// <returns>JSON string with test results</returns>
        Task<string> TestPasswordRecognizerAsync();

        /// <summary>
        /// Tests a custom pattern against provided text
        /// </summary>
        /// <param name="text">Text to test against</param>
        /// <param name="pattern">Custom pattern to test</param>
        /// <returns>JSON string with test results</returns>
        Task<string> TestCustomPatternAsync(string text, CustomPatternConfiguration pattern);

        /// <summary>
        /// Validates a custom pattern configuration
        /// </summary>
        /// <param name="pattern">Pattern to validate</param>
        /// <returns>JSON string with validation results</returns>
        Task<string> ValidateCustomPatternAsync(CustomPatternConfiguration pattern);
    }
}