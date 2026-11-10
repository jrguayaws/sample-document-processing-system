using System;
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace DocumentProcessor.WebForms.Configuration
{
    /// <summary>
    /// Typed wrapper over <see cref="IConfiguration"/> that keeps the legacy Web.config key
    /// names (Bedrock.*, Storage.*, Database.*) now stored as flat keys in appsettings.json.
    /// Registered as a singleton in Program.cs; inject it instead of reading the old static class.
    /// A bad value still surfaces as a FormatException the first time the setting is touched.
    /// </summary>
    public class AppSettings
    {
        private readonly IConfiguration _configuration;

        public AppSettings(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public string ConnectionString
        {
            get
            {
                var value = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(value))
                {
                    throw new InvalidOperationException(
                        "The 'DefaultConnection' connection string is missing from configuration (ConnectionStrings:DefaultConnection).");
                }

                return value;
            }
        }

        public bool DatabaseUseSecretsManager
        {
            get { return GetBoolean("Database.UseSecretsManager", false); }
        }

        public string DatabaseSecretDescriptionPrefix
        {
            get { return GetString("Database.SecretDescriptionPrefix", "Password for RDS MSSQL used for MAM417."); }
        }

        public string BedrockRegion
        {
            get { return GetString("Bedrock.Region", "us-east-1"); }
        }

        public string BedrockSummarizationModelId
        {
            get { return GetString("Bedrock.SummarizationModelId", "global.anthropic.claude-sonnet-5"); }
        }

        public int BedrockMaxTokens
        {
            get { return GetInt32("Bedrock.MaxTokens", 2000); }
        }

        /// <summary>Characters of extracted text sent to the model.</summary>
        public int BedrockMaxInputCharacters
        {
            get { return GetInt32("Bedrock.MaxInputCharacters", 10000); }
        }

        public int BedrockMaxPdfPages
        {
            get { return GetInt32("Bedrock.MaxPdfPages", 5); }
        }

        /// <summary>
        /// App-relative path ("~/App_Data/uploads") or an absolute path. Resolve it against
        /// IWebHostEnvironment.ContentRootPath with <see cref="ResolveContentPath"/>.
        /// </summary>
        public string StorageRootPath
        {
            get { return GetString("Storage.RootPath", "~/App_Data/uploads"); }
        }

        public int StorageMaxFileSizeMegabytes
        {
            get { return GetInt32("Storage.MaxFileSizeMegabytes", 50); }
        }

        public long StorageMaxFileSizeBytes
        {
            get { return StorageMaxFileSizeMegabytes * 1024L * 1024L; }
        }

        public int StorageMaxFilesPerUpload
        {
            get { return GetInt32("Storage.MaxFilesPerUpload", 10); }
        }

        /// <summary>
        /// Largest request body the host accepts: every file of one upload at the maximum size.
        /// Replaces httpRuntime maxRequestLength / requestLimits maxAllowedContentLength.
        /// </summary>
        public long StorageMaxRequestBodyBytes
        {
            get { return StorageMaxFilesPerUpload * StorageMaxFileSizeBytes; }
        }

        public string[] StorageAllowedExtensions
        {
            get
            {
                var raw = GetString("Storage.AllowedExtensions", ".pdf,.txt,.log");
                return raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
        }

        /// <summary>
        /// Replacement for HostingEnvironment.MapPath: maps "~/x/y" (or a relative path) onto the
        /// content root; absolute paths are returned unchanged.
        /// </summary>
        public static string ResolveContentPath(string contentRootPath, string path)
        {
            var relative = path.StartsWith("~", StringComparison.Ordinal)
                ? path.Substring(1).TrimStart('/', '\\')
                : path;

            relative = relative.Replace('/', System.IO.Path.DirectorySeparatorChar)
                               .Replace('\\', System.IO.Path.DirectorySeparatorChar);

            return System.IO.Path.GetFullPath(System.IO.Path.Combine(contentRootPath, relative));
        }

        private string GetString(string key, string fallback)
        {
            var value = _configuration[key];
            return string.IsNullOrEmpty(value) ? fallback : value.Trim();
        }

        private int GetInt32(string key, int fallback)
        {
            var value = _configuration[key];
            return string.IsNullOrEmpty(value) ? fallback : int.Parse(value.Trim(), CultureInfo.InvariantCulture);
        }

        private bool GetBoolean(string key, bool fallback)
        {
            var value = _configuration[key];
            return string.IsNullOrEmpty(value) ? fallback : bool.Parse(value.Trim());
        }
    }
}
