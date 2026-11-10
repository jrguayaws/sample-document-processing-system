using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using DocumentProcessor.WebForms.Configuration;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;

namespace DocumentProcessor.WebForms.Data
{
    /// <summary>
    /// Works out the SQL Server connection string at application start, either from
    /// configuration (ConnectionStrings:DefaultConnection) or from AWS Secrets Manager.
    /// </summary>
    public class DatabaseConnectionResolver
    {
        private readonly AppSettings _settings;
        private readonly Func<IAmazonSecretsManager> _secretsManager;

        /// <param name="settings">Application settings.</param>
        /// <param name="secretsManager">
        /// Supplies the Secrets Manager client. A factory, so the client is only built when
        /// Database.UseSecretsManager is on, and a client that cannot be built (no region,
        /// no credentials) falls back to configuration exactly like a failed lookup did.
        /// </param>
        public DatabaseConnectionResolver(AppSettings settings, Func<IAmazonSecretsManager> secretsManager)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _secretsManager = secretsManager ?? throw new ArgumentNullException(nameof(secretsManager));
        }

        public async Task<DatabaseConnection> ResolveAsync(CancellationToken cancellationToken = default)
        {
            var configured = _settings.ConnectionString;
            var fromConfiguration = new DatabaseConnection(
                configured,
                new DatabaseInfo(DetectProvider(configured), "appsettings.json", HostOf(configured)));

            if (!_settings.DatabaseUseSecretsManager)
            {
                return fromConfiguration;
            }

            try
            {
                return await FromSecretsManagerAsync(_settings.DatabaseSecretDescriptionPrefix, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return new DatabaseConnection(
                    fromConfiguration.ConnectionString,
                    fromConfiguration.Info,
                    "Secrets Manager lookup failed (" + ex.Message + "); using the configured connection string.");
            }
        }

        private async Task<DatabaseConnection> FromSecretsManagerAsync(string descriptionPrefix, CancellationToken cancellationToken)
        {
            var client = _secretsManager();

            SecretListEntry match = null;
            string nextToken = null;
            do
            {
                var page = await client.ListSecretsAsync(new ListSecretsRequest { NextToken = nextToken }, cancellationToken)
                    .ConfigureAwait(false);

                match = page.SecretList?.FirstOrDefault(s =>
                    s.Description != null &&
                    s.Description.StartsWith(descriptionPrefix, StringComparison.OrdinalIgnoreCase));

                nextToken = page.NextToken;
            }
            while (match == null && !string.IsNullOrEmpty(nextToken));

            if (match == null)
            {
                throw new InvalidOperationException("No secret whose description starts with '" + descriptionPrefix + "'.");
            }

            var value = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = match.ARN }, cancellationToken)
                .ConfigureAwait(false);

            var secret = JObject.Parse(value.SecretString);
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = Field(secret, "host") + "," + Field(secret, "port"),
                InitialCatalog = Field(secret, "dbname"),
                UserID = Field(secret, "username"),
                Password = Field(secret, "password"),
                TrustServerCertificate = true,
                Encrypt = true
            };

            return new DatabaseConnection(
                builder.ConnectionString,
                new DatabaseInfo(DatabaseProvider.SqlServer, "AWS Secrets Manager", builder.DataSource));
        }

        /// <summary>
        /// Npgsql connection strings use Host=/Username=; SQL Server uses Server=/User Id=.
        /// Lets the UI report which engine is actually in use after a PostgreSQL migration.
        /// </summary>
        public static DatabaseProvider DetectProvider(string connectionString)
        {
            var looksPostgres = connectionString.IndexOf("Host=", StringComparison.OrdinalIgnoreCase) >= 0
                && connectionString.IndexOf("Server=", StringComparison.OrdinalIgnoreCase) < 0;
            return looksPostgres ? DatabaseProvider.PostgreSql : DatabaseProvider.SqlServer;
        }

        private static string HostOf(string connectionString)
        {
            if (DetectProvider(connectionString) == DatabaseProvider.SqlServer)
            {
                return new SqlConnectionStringBuilder(connectionString).DataSource;
            }

            var parts = connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var host = ValueOf(parts, "Host");
            var port = ValueOf(parts, "Port");
            if (host == null)
            {
                return "unknown";
            }

            return port == null ? host : host + ":" + port;
        }

        private static string ValueOf(string[] parts, string key)
        {
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed.Substring(key.Length + 1);
                }
            }

            return null;
        }

        private static string Field(JObject secret, string name)
        {
            var token = secret[name];
            if (token == null)
            {
                throw new InvalidOperationException("Secret is missing the '" + name + "' field.");
            }

            return token.ToString();
        }
    }
}
