using SecurePaste.Models;

namespace SecurePaste.Services
{
    /// <summary>
    /// Interface for configuration service
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Gets the current configuration
        /// </summary>
        Configuration GetConfiguration();

        /// <summary>
        /// Saves the configuration
        /// </summary>
        /// <param name="configuration">Configuration to save</param>
        void SaveConfiguration(Configuration configuration);

        /// <summary>
        /// Gets the current statistics
        /// </summary>
        Statistics GetStatistics();

        /// <summary>
        /// Updates statistics after an operation
        /// </summary>
        /// <param name="success">Whether the operation was successful</param>
        /// <param name="entitiesFound">Entities found during the operation</param>
        void UpdateStatistics(bool success, Dictionary<string, int>? entitiesFound = null);

        /// <summary>
        /// Resets all statistics
        /// </summary>
        void ResetStatistics();

        /// <summary>
        /// Gets the configuration file path
        /// </summary>
        string GetConfigurationFilePath();

        /// <summary>
        /// Gets the statistics file path
        /// </summary>
        string GetStatisticsFilePath();
    }
}