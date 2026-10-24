using System;
using System.Data;
using MySqlConnector;

namespace ShelfApi.Infrastructure;

public class MySqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public MySqlConnectionFactory(string connectionString)
    {
        _connectionString = NormalizeConnectionString(connectionString);
    }

    public IDbConnection CreateConnection()
    {
        return new MySqlConnection(_connectionString);
    }

    private static string NormalizeConnectionString(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return raw;
        }

        var trimmed = raw.Trim();
        if (trimmed.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase) &&
            Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            var userInfo = uri.UserInfo.Split(':', 2);
            var user = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
            var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var db = uri.AbsolutePath.Trim('/');
            if (string.IsNullOrWhiteSpace(db)) db = "defaultdb";

            var builder = new MySqlConnectionStringBuilder
            {
                Server = uri.Host,
                Port = (uint)(uri.Port > 0 ? uri.Port : 3306),
                UserID = user,
                Password = pass,
                Database = db,
                SslMode = MySqlSslMode.Required
            };
            return builder.ConnectionString;
        }

        return trimmed;
    }
}