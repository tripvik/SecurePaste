using Newtonsoft.Json;
using SecurePaste.Models;

namespace SecurePaste.Services
{
    /// <summary>
    /// Service for managing application configuration
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly string _configFilePath;
        private readonly string _statisticsFilePath;
        private Configuration? _configuration;
        private Statistics? _statistics;

        public ConfigurationService()
        {
            var appDataPath = Path.Combine(Environment.CurrentDirectory, "SecurePaste");
            Directory.CreateDirectory(appDataPath);
            
            _configFilePath = Path.Combine(appDataPath, "config.json");
            _statisticsFilePath = Path.Combine(appDataPath, "statistics.json");
        }

        /// <summary>
        /// Gets the current configuration
        /// </summary>
        public Configuration GetConfiguration()
        {
            if (_configuration == null)
            {
                LoadConfiguration();
            }
            return _configuration!;
        }

        /// <summary>
        /// Saves the configuration
        /// </summary>
        /// <param name="configuration">Configuration to save</param>
        public void SaveConfiguration(Configuration configuration)
        {
            _configuration = configuration;
            
            try
            {
                var json = JsonConvert.SerializeObject(configuration, Formatting.Indented);
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads configuration from file or creates default
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    var loadedConfig = JsonConvert.DeserializeObject<Configuration>(json);
                    
                    if (loadedConfig != null)
                    {
                        _configuration = loadedConfig;
                        
                        // Ensure entities are properly initialized - prevent duplication
                        // If entities list is empty or null, initialize with defaults
                        if (_configuration.Entities == null || _configuration.Entities.Count == 0)
                        {
                            _configuration.Entities = Configuration.GetDefaultEntityConfigurations();
                        }
                        else
                        {
                            // Ensure all supported entities are present by merging with defaults
                            // This handles cases where new entities are added in updates
                            EnsureAllEntitiesPresent(_configuration);
                        }
                    }
                    else
                    {
                        _configuration = new Configuration();
                    }
                }
                else
                {
                    _configuration = new Configuration();
                    SaveConfiguration(_configuration);
                }
            }
            catch
            {
                _configuration = new Configuration();
            }
        }

        /// <summary>
        /// Ensures all supported entities are present in the configuration
        /// Adds missing entities without duplicating existing ones
        /// </summary>
        private void EnsureAllEntitiesPresent(Configuration config)
        {
            var defaultEntities = Configuration.GetDefaultEntityConfigurations();
            var existingEntityTypes = config.Entities.Select(e => e.Type).ToHashSet();
            
            // Add any missing entities from the default list
            foreach (var defaultEntity in defaultEntities)
            {
                if (!existingEntityTypes.Contains(defaultEntity.Type))
                {
                    config.Entities.Add(defaultEntity);
                }
            }
            
            // Remove any entities that are no longer in the default list (cleanup)
            var validEntityTypes = defaultEntities.Select(e => e.Type).ToHashSet();
            config.Entities.RemoveAll(e => !validEntityTypes.Contains(e.Type));
        }

        /// <summary>
        /// Gets the current statistics
        /// </summary>
        public Statistics GetStatistics()
        {
            if (_statistics == null)
            {
                LoadStatistics();
            }
            return _statistics!;
        }

        /// <summary>
        /// Updates statistics after an operation
        /// </summary>
        /// <param name="success">Whether the operation was successful</param>
        /// <param name="entitiesFound">Entities found during the operation</param>
        public void UpdateStatistics(bool success, Dictionary<string, int>? entitiesFound = null)
        {
            var stats = GetStatistics();
            
            stats.TotalOperations++;
            if (success)
                stats.SuccessfulOperations++;
            else
                stats.FailedOperations++;

            stats.LastOperation = DateTime.Now;

            if (entitiesFound != null)
            {
                foreach (var entity in entitiesFound)
                {
                    if (stats.EntitiesFound.ContainsKey(entity.Key))
                        stats.EntitiesFound[entity.Key] += entity.Value;
                    else
                        stats.EntitiesFound[entity.Key] = entity.Value;
                }
            }

            SaveStatistics(stats);
        }

        /// <summary>
        /// Saves statistics to file
        /// </summary>
        /// <param name="statistics">Statistics to save</param>
        private void SaveStatistics(Statistics statistics)
        {
            _statistics = statistics;
            
            try
            {
                var json = JsonConvert.SerializeObject(statistics, Formatting.Indented);
                File.WriteAllText(_statisticsFilePath, json);
            }
            catch
            {
                // Ignore save errors for statistics
            }
        }

        /// <summary>
        /// Loads statistics from file or creates default
        /// </summary>
        private void LoadStatistics()
        {
            try
            {
                if (File.Exists(_statisticsFilePath))
                {
                    var json = File.ReadAllText(_statisticsFilePath);
                    _statistics = JsonConvert.DeserializeObject<Statistics>(json) ?? new Statistics();
                }
                else
                {
                    _statistics = new Statistics();
                }
            }
            catch
            {
                _statistics = new Statistics();
            }
        }

        /// <summary>
        /// Resets all statistics
        /// </summary>
        public void ResetStatistics()
        {
            _statistics = new Statistics();
            SaveStatistics(_statistics);
        }

        /// <summary>
        /// Gets the configuration file path
        /// </summary>
        public string GetConfigurationFilePath() => _configFilePath;

        /// <summary>
        /// Gets the statistics file path
        /// </summary>
        public string GetStatisticsFilePath() => _statisticsFilePath;
    }
}