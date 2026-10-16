using System;
using System.Linq;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using DocumentProcessor.WebForms.Configuration;
using Newtonsoft.Json.Linq;
using Microsoft.Data.SqlClient;

namespace DocumentProcessor.WebForms.Data
{
    /// <summary>
    /// Works out the SQL Server connection string at application start, either from
    /// appsettings.json or from AWS Secrets Manager.
    /// </summary>
    public static class DatabaseConnectionResolver
    {
        /// <summary>
        /// Resolves the connection string using the configured value and database options.
        /// If <paramref name="options"/> enables Secrets Manager, attempts to fetch credentials
        /// from AWS; falls back to the configured string on failure.
        /// </summary>
        public static DatabaseConnection Resolve(string configuredConnectionString, DatabaseOptions options)
        {
            var fromConfiguration = new DatabaseConnection(
                configuredConnectionString,
                new DatabaseInfo(DetectProvider(configuredConnectionString), "appsettings.json", HostOf(configuredConnectionString)));

            if (!options.UseSecretsManager)
            {
                return fromConfiguration;
            }

            try
            {
                return FromSecretsManager(options.SecretDescriptionPrefix);
            }
            catch (Exception ex)
            {
                return new DatabaseConnection(
                    fromConfiguration.ConnectionString,
                    fromConfiguration.Info,
                    "Secrets Manager lookup failed (" + ex.Message + "); using the configured connection string.");
            }
        }

        private static DatabaseConnection FromSecretsManager(string descriptionPrefix)
        {
            using (var client = new AmazonSecretsManagerClient())
            {
                var secrets = client.ListSecretsAsync(new ListSecretsRequest()).GetAwaiter().GetResult();
                var match = secrets.SecretList.FirstOrDefault(s =>
                    s.Description != null &&
                    s.Description.StartsWith(descriptionPrefix, StringComparison.OrdinalIgnoreCase));

                if (match == null)
                {
                    throw new InvalidOperationException("No secret whose description starts with '" + descriptionPrefix + "'.");
                }

                var value = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = match.ARN }).GetAwaiter().GetResult();
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

        private static string? ValueOf(string[] parts, string key)
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
