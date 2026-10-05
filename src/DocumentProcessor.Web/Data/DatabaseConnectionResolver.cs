using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using DocumentProcessor.Web.Configuration;
using Microsoft.Data.SqlClient;

namespace DocumentProcessor.Web.Data;

/// <summary>
/// Resolves the SQL Server connection string at startup, either from configuration or
/// from AWS Secrets Manager. Runs before the DI container exists, so it takes no services.
/// </summary>
public static class DatabaseConnectionResolver
{
    public static async Task<DatabaseConnection> ResolveAsync(
        string configuredConnectionString,
        DatabaseOptions options,
        CancellationToken cancellationToken = default)
    {
        var fromConfiguration = new DatabaseConnection(
            configuredConnectionString,
            new DatabaseInfo(
                DetectProvider(configuredConnectionString),
                "appsettings.json",
                HostOf(configuredConnectionString)));

        if (!options.UseSecretsManager)
        {
            return fromConfiguration;
        }

        try
        {
            return await FromSecretsManagerAsync(options.SecretDescriptionPrefix, cancellationToken);
        }
        catch (Exception ex)
        {
            return fromConfiguration with
            {
                Warning = $"Secrets Manager lookup failed ({ex.Message}); using the configured connection string."
            };
        }
    }

    private static async Task<DatabaseConnection> FromSecretsManagerAsync(
        string descriptionPrefix,
        CancellationToken cancellationToken)
    {
        using var client = new AmazonSecretsManagerClient();

        var secrets = await client.ListSecretsAsync(new ListSecretsRequest(), cancellationToken);
        var match = secrets.SecretList.FirstOrDefault(s =>
            s.Description?.StartsWith(descriptionPrefix, StringComparison.OrdinalIgnoreCase) == true)
            ?? throw new InvalidOperationException($"No secret whose description starts with '{descriptionPrefix}'.");

        var value = await client.GetSecretValueAsync(
            new GetSecretValueRequest { SecretId = match.ARN }, cancellationToken);

        using var json = JsonDocument.Parse(value.SecretString);
        var root = json.RootElement;

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{Field(root, "host")},{Field(root, "port")}",
            InitialCatalog = Field(root, "dbname"),
            UserID = Field(root, "username"),
            Password = Field(root, "password"),
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
    public static DatabaseProvider DetectProvider(string connectionString) =>
        connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
        && !connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)
            ? DatabaseProvider.PostgreSql
            : DatabaseProvider.SqlServer;

    private static string HostOf(string connectionString)
    {
        if (DetectProvider(connectionString) is DatabaseProvider.SqlServer)
        {
            return new SqlConnectionStringBuilder(connectionString).DataSource;
        }

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var host = Value(parts, "Host") ?? "unknown";
        var port = Value(parts, "Port");

        return port is null ? host : $"{host}:{port}";

        static string? Value(string[] parts, string key) => parts
            .FirstOrDefault(p => p.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))?
            .Split('=', 2)[1];
    }

    private static string Field(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value)
            ? value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? string.Empty,
                JsonValueKind.Number => value.GetRawText(),
                _ => value.ToString()
            }
            : throw new InvalidOperationException($"Secret is missing the '{name}' field.");
}
