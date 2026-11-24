using System.Data;
using Microsoft.Data.Sqlite;
using OpsDesk.Application.Common.Interfaces;

namespace OpsDesk.Infrastructure.Persistence.Db;

public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString) => _connectionString = connectionString;

    public IDbConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
