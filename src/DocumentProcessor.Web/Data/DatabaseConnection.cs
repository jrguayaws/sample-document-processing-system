namespace DocumentProcessor.Web.Data;

public enum DatabaseProvider { SqlServer, PostgreSql }

/// <summary>Where the app connected, surfaced in the UI banner and footer.</summary>
public sealed record DatabaseInfo(
    DatabaseProvider Provider,
    string CredentialSource,
    string HostAddress)
{
    public string DisplayName => Provider switch
    {
        DatabaseProvider.PostgreSql => "PostgreSQL",
        _ => "SQL Server"
    };

    /// <summary>"localhost" for a local instance, otherwise "remote" — the full host and port are noise in the banner.</summary>
    public string LocationLabel => IsLocal(HostAddress) ? "localhost" : "remote";

    private static bool IsLocal(string hostAddress)
    {
        if (string.IsNullOrWhiteSpace(hostAddress))
        {
            return false;
        }

        // Strip protocol prefix (tcp:), port (",1433" / ":5432") and named instance ("\SQLEXPRESS").
        var host = hostAddress.Trim();
        if (host.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
        {
            host = host[4..];
        }

        var end = host.IndexOfAny([',', '\\']);
        var colon = host.IndexOf(':');
        if (end < 0 && colon > 0 && host.IndexOf(':', colon + 1) < 0)
        {
            end = colon;
        }

        if (end >= 0)
        {
            host = host[..end];
        }

        return host.Trim().ToLowerInvariant() switch
        {
            "localhost" or "127.0.0.1" or "::1" or "." or "(local)" or "(localdb)" => true,
            _ => host.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)
        };
    }
}

public sealed record DatabaseConnection(string ConnectionString, DatabaseInfo Info, string? Warning = null);
