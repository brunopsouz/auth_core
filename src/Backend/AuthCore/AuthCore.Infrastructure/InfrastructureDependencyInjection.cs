using AuthCore.Domain.Common.Repositories;
using AuthCore.Domain.Passports.Repositories;
using AuthCore.Domain.Security.Cryptography;
using AuthCore.Domain.Security.Tokens.Services;
using AuthCore.Domain.Users.Repositories;
using AuthCore.Infrastructure.Abstractions.Data;
using AuthCore.Infrastructure.Configurations;
using AuthCore.Infrastructure.Persistences.Migrations.Versions;
using AuthCore.Infrastructure.Persistences.Read.PostgreSQL.Repositories;
using AuthCore.Infrastructure.Persistences.Write.PostgreSQL.Connections;
using AuthCore.Infrastructure.Persistences.Write.PostgreSQL.Repositories;
using AuthCore.Infrastructure.Persistences.Write.PostgreSQL.UnitOfWork;
using AuthCore.Infrastructure.Security.Cryptography;
using AuthCore.Infrastructure.Security.Tokens;
using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthCore.Infrastructure;

/// <summary>
/// Define operações para registrar dependências da infraestrutura.
/// </summary>
public static class InfrastructureDependencyInjection
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

        AddOptions(services, configuration);
        AddPersistence(services);
        AddSecurity(services);
        AddRepositories(services);
        AddMigrations(services, configuration);

        return services;
    }

    #region Helpers

    /// <summary>
    /// Operação para adicionar as opções de configuração da infraestrutura.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    private static void AddOptions(IServiceCollection services, IConfiguration configuration)
    {
        AddDatabaseOptions(services, configuration);
        AddDatabaseMigrationOptions(services, configuration);
        AddJwtOptions(services, configuration);
        AddRedisOptions(services, configuration);
        AddRabbitMqOptions(services, configuration);
    }

    /// <summary>
    /// Operação para adicionar as dependências de persistência.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    private static void AddPersistence(IServiceCollection services)
    {
        services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();
        services.AddScoped<NpgsqlUnitOfWork>();
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<NpgsqlUnitOfWork>());
        services.AddScoped<IDatabaseSession>(serviceProvider => serviceProvider.GetRequiredService<NpgsqlUnitOfWork>());
    }

    /// <summary>
    /// Operação para adicionar os serviços de segurança.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    private static void AddSecurity(IServiceCollection services)
    {
        services.AddScoped<IPasswordEncripter, BCryptNet>();
        services.AddScoped<IAccessTokenGenerator, JwtAccessTokenGenerator>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
    }

    /// <summary>
    /// Operação para adicionar os repositórios da infraestrutura.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<UserRepository>();
        services.AddScoped<UserReadRepository>();
        services.AddScoped<IUserRepository>(serviceProvider => serviceProvider.GetRequiredService<UserRepository>());
        services.AddScoped<IUserReadRepository>(serviceProvider => serviceProvider.GetRequiredService<UserReadRepository>());
        services.AddScoped<IPasswordRepository, PasswordRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    }

    /// <summary>
    /// Operação para adicionar o FluentMigrator.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    private static void AddMigrations(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = GetPostgreSqlConnectionString(configuration);

        services
            .AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(DatabaseVersions).Assembly).For.Migrations());
    }

    /// <summary>
    /// Operação para adicionar as opções de banco de dados.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    private static void AddDatabaseOptions(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName));
    }

    /// <summary>
    /// Operação para adicionar as opções de migração do banco.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    private static void AddDatabaseMigrationOptions(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<DatabaseMigrationOptions>()
            .Bind(configuration.GetSection(DatabaseMigrationOptions.SectionName))
            .ValidateDataAnnotations();
    }

    /// <summary>
    /// Operação para adicionar as opções de JWT.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    private static void AddJwtOptions(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations();
    }

    /// <summary>
    /// Operação para adicionar as opções de Redis.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    private static void AddRedisOptions(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName))
            .ValidateDataAnnotations();
    }

    /// <summary>
    /// Operação para adicionar as opções de RabbitMQ.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    private static void AddRabbitMqOptions(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations();
    }

    /// <summary>
    /// Operação para obter a connection string do PostgreSQL.
    /// </summary>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>Connection string configurada para o PostgreSQL.</returns>
    private static string GetPostgreSqlConnectionString(IConfiguration configuration)
    {
        return configuration.GetConnectionString("PostgreSql")
            ?? configuration.GetSection(DatabaseOptions.SectionName).GetValue<string>(nameof(DatabaseOptions.PostgreSql))
            ?? string.Empty;
    }

    #endregion
}
