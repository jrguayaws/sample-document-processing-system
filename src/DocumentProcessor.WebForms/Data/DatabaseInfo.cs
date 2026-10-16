namespace DocumentProcessor.WebForms.Data
{
    public enum DatabaseProvider
    {
        SqlServer = 0,
        PostgreSql = 1
    }

    /// <summary>Where the app connected, surfaced in the UI banner and footer.</summary>
    public class DatabaseInfo
    {
        public DatabaseInfo(DatabaseProvider provider, string credentialSource, string hostAddress)
        {
            Provider = provider;
            CredentialSource = credentialSource;
            HostAddress = hostAddress;
        }

        public DatabaseProvider Provider { get; private set; }

        public string CredentialSource { get; private set; }

        public string HostAddress { get; private set; }

        public string DisplayName
        {
            get { return Provider == DatabaseProvider.PostgreSql ? "PostgreSQL" : "SQL Server"; }
        }

        /// <summary>"localhost" for a local instance, otherwise "remote" — the full host and port are noise in the banner.</summary>
        public string LocationLabel
        {
            get { return IsLocal(HostAddress) ? "localhost" : "remote"; }
        }

        private static bool IsLocal(string hostAddress)
        {
            if (string.IsNullOrWhiteSpace(hostAddress))
            {
                return false;
            }

            // Strip protocol prefix (tcp:), port (",1433" / ":5432") and named instance ("\SQLEXPRESS").
            var host = hostAddress.Trim();
            var colon = host.IndexOf(':');
            if (host.StartsWith("tcp:", System.StringComparison.OrdinalIgnoreCase))
            {
                host = host.Substring(4);
                colon = host.IndexOf(':');
            }

            var end = host.IndexOfAny(new[] { ',', '\\' });
            if (end < 0 && colon > 0 && host.IndexOf(':', colon + 1) < 0)
            {
                end = colon;
            }

            if (end >= 0)
            {
                host = host.Substring(0, end);
            }

            switch (host.Trim().ToLowerInvariant())
            {
                case "localhost":
                case "127.0.0.1":
                case "::1":
                case ".":
                case "(local)":
                case "(localdb)":
                    return true;
                default:
                    return host.Equals(System.Environment.MachineName, System.StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    public class DatabaseConnection
    {
        public DatabaseConnection(string connectionString, DatabaseInfo info)
            : this(connectionString, info, null)
        {
        }

        public DatabaseConnection(string connectionString, DatabaseInfo info, string warning)
        {
            ConnectionString = connectionString;
            Info = info;
            Warning = warning;
        }

        public string ConnectionString { get; private set; }

        public DatabaseInfo Info { get; private set; }

        /// <summary>Non-fatal problem worth logging at startup; null when everything resolved cleanly.</summary>
        public string Warning { get; private set; }
    }
}
