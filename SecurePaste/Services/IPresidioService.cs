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
    }
}