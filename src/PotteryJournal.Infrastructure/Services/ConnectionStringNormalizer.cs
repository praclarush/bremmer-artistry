using System;
using Npgsql;

namespace PotteryJournal.Infrastructure.Services
{
    /// <summary>
    /// Converts a <c>postgres://</c> / <c>postgresql://</c> URI connection string into the
    /// key/value form Npgsql understands. Managed Postgres hosts (Render, Heroku, and similar) hand
    /// out the URI form, which Npgsql's connection string builder rejects.
    /// </summary>
    public static class ConnectionStringNormalizer
    {
        private const string POSTGRES_SCHEME = "postgres";
        private const string POSTGRESQL_SCHEME = "postgresql";
        private const char USER_INFO_SEPARATOR = ':';
        private const char PATH_SEPARATOR = '/';
        private const int PORT_NOT_SPECIFIED = -1;

        /// <summary>
        /// Returns the connection string in Npgsql key/value form. A value that isn't a Postgres URI
        /// (including null, blank, or an already key/value string) is returned unchanged. Query
        /// string parameters on the URI are not carried over.
        /// </summary>
        /// <param name="connectionString">The configured connection string, in either form.</param>
        public static string? Normalize(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)
                || !Uri.TryCreate(connectionString, UriKind.Absolute, out Uri? uri)
                || !IsPostgresScheme(uri.Scheme))
            {
                return connectionString;
            }

            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart(PATH_SEPARATOR)),
            };

            if (uri.Port != PORT_NOT_SPECIFIED)
            {
                builder.Port = uri.Port;
            }

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                string[] userInfo = uri.UserInfo.Split(USER_INFO_SEPARATOR, 2);
                builder.Username = Uri.UnescapeDataString(userInfo[0]);
                if (userInfo.Length > 1)
                {
                    builder.Password = Uri.UnescapeDataString(userInfo[1]);
                }
            }

            return builder.ConnectionString;
        }

        private static bool IsPostgresScheme(string scheme)
        {
            return string.Equals(scheme, POSTGRES_SCHEME, StringComparison.OrdinalIgnoreCase)
                || string.Equals(scheme, POSTGRESQL_SCHEME, StringComparison.OrdinalIgnoreCase);
        }
    }
}
