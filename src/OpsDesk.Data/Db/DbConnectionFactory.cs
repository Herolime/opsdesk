using System.Data;
using Microsoft.Data.Sqlite;
using OpsDesk.Application.Common.Interfaces;

namespace OpsDesk.Data.Db;

public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(string connectionString) => _connectionString = connectionString;

    public IDbConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
