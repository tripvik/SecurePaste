using Newtonsoft.Json;

namespace SecurePaste.Models
{
    /// <summary>
    /// Application configuration settings
    /// </summary>
    public class Configuration
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("entities")]
        public List<EntityConfiguration> Entities { get; set; } = new List<EntityConfiguration>();

        [JsonProperty("custom_patterns")]
        public List<CustomPatternConfiguration> CustomPatterns { get; set; } = new List<CustomPatternConfiguration>();

        [JsonProperty("confidence_threshold")]
        public double ConfidenceThreshold { get; set; } = 0.7;

        [JsonProperty("python_path")]
        public string PythonPath { get; set; } = "python";

        [JsonProperty("presidio_installed")]
        public bool PresidioInstalled { get; set; } = false;

        [JsonProperty("notifications_enabled")]
        public bool NotificationsEnabled { get; set; } = true;

        [JsonProperty("auto_start")]
        public bool AutoStart { get; set; } = false;

        [JsonProperty("language")]
        public string Language { get; set; } = "en";

        /// <summary>
        /// Constructor that initializes entities with defaults if not loaded from JSON
        /// </summary>
        public Configuration()
        {
            InitializeDefaultEntities();
            InitializeDefaultCustomPatterns();
        }

        /// <summary>
        /// Initializes the default entity configuration with all supported Presidio entities enabled
        /// </summary>
        private void InitializeDefaultEntities()
        {
            if (Entities == null || Entities.Count == 0)
            {
                Entities = GetDefaultEntityConfigurations();
            }
        }

        /// <summary>
        /// Initializes default custom patterns
        /// </summary>
        private void InitializeDefaultCustomPatterns()
        {
            if (CustomPatterns == null || CustomPatterns.Count == 0)
            {
                CustomPatterns = GetDefaultCustomPatterns();
            }
        }

        /// <summary>
        /// Gets the complete list of default entity configurations with all supported Presidio entities enabled by default
        /// </summary>
        public static List<EntityConfiguration> GetDefaultEntityConfigurations()
        {
            return new List<EntityConfiguration>
            {
                // Core PII entities - all enabled by default
                new EntityConfiguration { Type = "PERSON", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "EMAIL_ADDRESS", Enabled = true, AnonymizationMethod = "mask" },
                new EntityConfiguration { Type = "PHONE_NUMBER", Enabled = true, AnonymizationMethod = "mask" },
                new EntityConfiguration { Type = "CREDIT_CARD", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "IBAN_CODE", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "IP_ADDRESS", Enabled = true, AnonymizationMethod = "mask" },
                new EntityConfiguration { Type = "LOCATION", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "DATE_TIME", Enabled = true, AnonymizationMethod = "mask" },
                new EntityConfiguration { Type = "URL", Enabled = true, AnonymizationMethod = "mask" },
                new EntityConfiguration { Type = "DOMAIN_NAME", Enabled = true, AnonymizationMethod = "mask" },
                
                // Security-related entities
                new EntityConfiguration { Type = "PASSWORD", Enabled = true, AnonymizationMethod = "replace" },
                
                // Government and ID entities
                new EntityConfiguration { Type = "US_SSN", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "US_PASSPORT", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "US_DRIVER_LICENSE", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "UK_NHS", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "NRP", Enabled = true, AnonymizationMethod = "replace" },
                
                // Financial entities
                new EntityConfiguration { Type = "CRYPTO", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "US_BANK_NUMBER", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "ABA_ROUTING_NUMBER", Enabled = true, AnonymizationMethod = "replace" },
                
                // Medical entities
                new EntityConfiguration { Type = "MEDICAL_LICENSE", Enabled = true, AnonymizationMethod = "replace" },
                
                // Organization entities
                new EntityConfiguration { Type = "ORG", Enabled = true, AnonymizationMethod = "replace" },
                
                // Other entities
                new EntityConfiguration { Type = "AGE", Enabled = true, AnonymizationMethod = "mask" },
                new EntityConfiguration { Type = "TITLE", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "AU_ABN", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "AU_ACN", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "AU_TFN", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "AU_MEDICARE", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "ES_NIF", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "IT_FISCAL_CODE", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "IT_DRIVER_LICENSE", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "IT_VAT_CODE", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "IT_PASSPORT", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "IT_IDENTITY_CARD", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "SG_NRIC_FIN", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "IN_PAN", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "IN_AADHAAR", Enabled = true, AnonymizationMethod = "replace" },
                new EntityConfiguration { Type = "IN_VOTER_NUMBER", Enabled = true, AnonymizationMethod = "replace" }
            };
        }

        /// <summary>
        /// Gets default custom pattern examples
        /// </summary>
        public static List<CustomPatternConfiguration> GetDefaultCustomPatterns()
        {
            return new List<CustomPatternConfiguration>
            {
                new CustomPatternConfiguration
                {
                    Name = "API Key Pattern",
                    Pattern = @"(?i)\b(?:api[_-]?key|apikey)\s*[:=]\s*['""]?([a-zA-Z0-9_-]{20,})['""]?",
                    EntityType = "API_KEY",
                    Enabled = false,
                    ConfidenceScore = 0.9,
                    AnonymizationMethod = "redact",
                    Description = "Detects API keys in various formats"
                },
                new CustomPatternConfiguration
                {
                    Name = "Database Connection String",
                    Pattern = @"(?i)(?:server|host|database|db)\s*=\s*[^;]+;.*(?:password|pwd)\s*=\s*[^;]+",
                    EntityType = "DB_CONNECTION",
                    Enabled = false,
                    ConfidenceScore = 0.8,
                    AnonymizationMethod = "redact",
                    Description = "Detects database connection strings"
                },
                new CustomPatternConfiguration
                {
                    Name = "JWT Token",
                    Pattern = @"\b(eyJ[a-zA-Z0-9_-]*\.eyJ[a-zA-Z0-9_-]*\.[a-zA-Z0-9_-]*)\b",
                    EntityType = "JWT_TOKEN",
                    Enabled = false,
                    ConfidenceScore = 0.95,
                    AnonymizationMethod = "redact",
                    Description = "Detects JSON Web Tokens (JWT)"
                }
            };
        }

        /// <summary>
        /// Resets entities and custom patterns to default configuration
        /// </summary>
        public void ResetToDefaults()
        {
            Entities = GetDefaultEntityConfigurations();
            CustomPatterns = GetDefaultCustomPatterns();
            Enabled = true;
            ConfidenceThreshold = 0.7;
            PythonPath = "python";
            NotificationsEnabled = true;
            AutoStart = false;
            Language = "en";
        }
    }

    /// <summary>
    /// Configuration for individual entity types
    /// </summary>
    public class EntityConfiguration
    {
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("anonymization_method")]
        public string AnonymizationMethod { get; set; } = "replace";

        [JsonProperty("custom_replacement")]
        public string? CustomReplacement { get; set; }
    }

    /// <summary>
    /// Configuration for custom regex patterns
    /// </summary>
    public class CustomPatternConfiguration
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("pattern")]
        public string Pattern { get; set; } = string.Empty;

        [JsonProperty("entity_type")]
        public string EntityType { get; set; } = string.Empty;

        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("confidence_score")]
        public double ConfidenceScore { get; set; } = 0.8;

        [JsonProperty("anonymization_method")]
        public string AnonymizationMethod { get; set; } = "replace";

        [JsonProperty("custom_replacement")]
        public string? CustomReplacement { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Statistics about anonymization operations
    /// </summary>
    public class Statistics
    {
        [JsonProperty("total_operations")]
        public int TotalOperations { get; set; }

        [JsonProperty("successful_operations")]
        public int SuccessfulOperations { get; set; }

        [JsonProperty("failed_operations")]
        public int FailedOperations { get; set; }

        [JsonProperty("entities_found")]
        public Dictionary<string, int> EntitiesFound { get; set; } = new Dictionary<string, int>();

        [JsonProperty("last_operation")]
        public DateTime? LastOperation { get; set; }
    }
}