namespace DocumentProcessor.Web.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// When true, database credentials are read from AWS Secrets Manager instead of
    /// the configured connection string.
    /// </summary>
    public bool UseSecretsManager { get; init; }

    public string SecretDescriptionPrefix { get; init; } = "Password for Aurora PostgreSQL used for MAM417.";
}
