namespace DocumentProcessor.Web.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// When true, SQL Server credentials are read from AWS Secrets Manager instead of
    /// the configured connection string.
    /// </summary>
    public bool UseSecretsManager { get; init; }

    public string SecretDescriptionPrefix { get; init; } = "Password for RDS MSSQL used for MAM319.";
}
