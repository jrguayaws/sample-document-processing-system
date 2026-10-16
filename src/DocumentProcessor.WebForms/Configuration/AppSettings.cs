namespace DocumentProcessor.WebForms.Configuration
{
    /// <summary>
    /// Database-related settings previously read from Web.config via ConfigurationManager.
    /// Bind to the "Database" section in appsettings.json.
    /// Register in DI: <c>builder.Services.Configure&lt;DatabaseOptions&gt;(builder.Configuration.GetSection("Database"));</c>
    /// The "DefaultConnection" connection string is read separately via
    /// <c>builder.Configuration.GetConnectionString("DefaultConnection")</c>.
    /// </summary>
    public class DatabaseOptions
    {
        public const string SectionName = "Database";

        public bool UseSecretsManager { get; set; } = false;

        public string SecretDescriptionPrefix { get; set; } = "Password for RDS MSSQL used for MAM319.";
    }

    /// <summary>
    /// Bedrock AI settings previously read from Web.config via ConfigurationManager.
    /// Bind to the "Bedrock" section in appsettings.json.
    /// Register in DI: <c>builder.Services.Configure&lt;BedrockOptions&gt;(builder.Configuration.GetSection("Bedrock"));</c>
    /// </summary>
    public class BedrockOptions
    {
        public const string SectionName = "Bedrock";

        public string Region { get; set; } = "us-east-1";

        public string SummarizationModelId { get; set; } = "global.anthropic.claude-sonnet-5";

        public int MaxTokens { get; set; } = 2000;

        /// <summary>Characters of extracted text sent to the model.</summary>
        public int MaxInputCharacters { get; set; } = 10000;

        public int MaxPdfPages { get; set; } = 5;
    }

    /// <summary>
    /// Storage settings previously read from Web.config via ConfigurationManager.
    /// Bind to the "Storage" section in appsettings.json.
    /// Register in DI: <c>builder.Services.Configure&lt;StorageOptions&gt;(builder.Configuration.GetSection("Storage"));</c>
    /// </summary>
    public class StorageOptions
    {
        public const string SectionName = "Storage";

        /// <summary>
        /// Root path for document storage. On legacy Web Forms this was a virtual path
        /// resolved with HostingEnvironment.MapPath; on modern .NET use a relative or
        /// absolute file-system path (e.g. "App_Data/uploads").
        /// </summary>
        public string RootPath { get; set; } = "App_Data/uploads";

        public int MaxFileSizeMegabytes { get; set; } = 50;

        public long MaxFileSizeBytes => MaxFileSizeMegabytes * 1024L * 1024L;

        public int MaxFilesPerUpload { get; set; } = 10;

        public string AllowedExtensions { get; set; } = ".pdf,.txt,.log";

        /// <summary>Returns the allowed extensions as an array, split on commas.</summary>
        public string[] GetAllowedExtensionsArray()
        {
            return (AllowedExtensions ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }


}