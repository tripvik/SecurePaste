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
        public List<EntityConfiguration> Entities { get; set; } = new List<EntityConfiguration>
        {
            new EntityConfiguration { Type = "PERSON", Enabled = true, AnonymizationMethod = "replace" },
            new EntityConfiguration { Type = "EMAIL_ADDRESS", Enabled = true, AnonymizationMethod = "mask" },
            new EntityConfiguration { Type = "PHONE_NUMBER", Enabled = true, AnonymizationMethod = "mask" },
            new EntityConfiguration { Type = "CREDIT_CARD", Enabled = true, AnonymizationMethod = "redact" },
            new EntityConfiguration { Type = "IBAN_CODE", Enabled = true, AnonymizationMethod = "redact" },
            new EntityConfiguration { Type = "IP_ADDRESS", Enabled = false, AnonymizationMethod = "hash" },
            new EntityConfiguration { Type = "LOCATION", Enabled = false, AnonymizationMethod = "replace" },
            new EntityConfiguration { Type = "DATE_TIME", Enabled = false, AnonymizationMethod = "mask" },
            new EntityConfiguration { Type = "NRP", Enabled = true, AnonymizationMethod = "redact" },
            new EntityConfiguration { Type = "MEDICAL_LICENSE", Enabled = false, AnonymizationMethod = "redact" },
            new EntityConfiguration { Type = "URL", Enabled = false, AnonymizationMethod = "mask" }
        };

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