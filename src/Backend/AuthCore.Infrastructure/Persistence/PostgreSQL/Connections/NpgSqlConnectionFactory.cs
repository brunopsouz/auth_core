using System.Data;
using AuthCore.Application.Abstractions.Data;
using AuthCore.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AuthCore.Infrastructure.Persistence.PostgreSQL;

/// <summary>
/// Creates PostgreSQL connections using Npgsql.
/// </summary>
public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="NpgsqlConnectionFactory"/> class.
    /// </summary>
    /// <param name="options">The database options.</param>
    /// <exception cref="ArgumentNullException">Thrown when options are null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the connection string is not configured.</exception>
    public NpgsqlConnectionFactory(IOptions<DatabaseOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _connectionString = options.Value.PostgreSql;

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Database connection string was not configured.");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}