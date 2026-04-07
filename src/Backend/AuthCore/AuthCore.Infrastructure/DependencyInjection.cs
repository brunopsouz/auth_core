using AuthCore.Infrastructure.Abstractions.Data;
using AuthCore.Infrastructure.Configurations;
using AuthCore.Infrastructure.Persistences.Migrations.Versions;
using AuthCore.Infrastructure.Persistences.PostgreSQL.Connections;
using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthCore.Infrastructure;

/// <summary>
/// Define operações para registrar dependências da infraestrutura.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Operação para adicionar os serviços de infraestrutura.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>Coleção de serviços atualizada.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName));

        services
            .AddOptions<DatabaseMigrationOptions>()
            .Bind(configuration.GetSection(DatabaseMigrationOptions.SectionName))
            .ValidateDataAnnotations();

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations();

        services
            .AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName))
            .ValidateDataAnnotations();

        services
            .AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();

        var connectionString = configuration.GetConnectionString("PostgreSql")
            ?? configuration.GetSection(DatabaseOptions.SectionName).GetValue<string>(nameof(DatabaseOptions.PostgreSql))
            ?? string.Empty;

        services
            .AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(DatabaseVersions).Assembly).For.Migrations());

        return services;
    }
}
