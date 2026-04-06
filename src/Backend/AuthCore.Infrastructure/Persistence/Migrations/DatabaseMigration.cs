using AuthCore.Infrastructure.Configuration;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AuthCore.Infrastructure.Persistence.Migrations;

/// <summary>
/// Applies FluentMigrator migrations and ensures the PostgreSQL database exists when configured.
/// </summary>
public static class DatabaseMigration
{
    /// <summary>
    /// Applies database creation and versioned migrations for the authentication schema.
    /// </summary>
    /// <param name="serviceProvider">The application service provider.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task MigrateAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        await using var scope = serviceProvider.CreateAsyncScope();

        var scopedServices = scope.ServiceProvider;
        var logger = scopedServices.GetRequiredService<ILoggerFactory>().CreateLogger("AuthCore.Infrastructure.Migrations");
        var databaseOptions = scopedServices.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var migrationOptions = scopedServices.GetRequiredService<IOptions<DatabaseMigrationOptions>>().Value;

        if (!migrationOptions.AutoMigrateOnStartup)
        {
            logger.LogInformation("Automatic database migrations are disabled.");
            return;
        }

        if (migrationOptions.EnsureDatabaseCreated)
        {
            await EnsureDatabaseCreatedAsync(
                databaseOptions.PostgreSql,
                migrationOptions.AdminDatabase,
                cancellationToken);
        }

        var runner = scopedServices.GetRequiredService<IMigrationRunner>();

        logger.LogInformation("Running FluentMigrator migrations.");

        runner.ListMigrations();
        runner.MigrateUp();

        logger.LogInformation("Database migrations applied successfully.");
    }

    private static async Task EnsureDatabaseCreatedAsync(
        string connectionString,
        string adminDatabase,
        CancellationToken cancellationToken)
    {
        var targetBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = targetBuilder.Database;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("The PostgreSQL database name must be present in the connection string.");
        }

        targetBuilder.Database = adminDatabase;

        await using var connection = new NpgsqlConnection(targetBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string existsSql = "SELECT 1 FROM pg_database WHERE datname = @databaseName;";
        await using var existsCommand = new NpgsqlCommand(existsSql, connection);
        existsCommand.Parameters.AddWithValue("databaseName", databaseName);

        var databaseExists = await existsCommand.ExecuteScalarAsync(cancellationToken) is not null;

        if (databaseExists)
        {
            return;
        }

        var quotedDatabaseName = QuoteIdentifier(databaseName);
        await using var createDatabaseCommand = new NpgsqlCommand($"CREATE DATABASE {quotedDatabaseName};", connection);
        await createDatabaseCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string QuoteIdentifier(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
