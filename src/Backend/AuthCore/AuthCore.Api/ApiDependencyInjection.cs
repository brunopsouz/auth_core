using System.Security.Claims;
using System.Text;
using AuthCore.Api.Exceptions;
using AuthCore.Api.HealthChecks;
using AuthCore.Infrastructure.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace AuthCore.Api;

/// <summary>
/// Define operações para registrar dependências da API.
/// </summary>
public static class ApiDependencyInjection
{
    private const string AuthenticationScheme = JwtBearerDefaults.AuthenticationScheme;

    /// <summary>
    /// Operação para adicionar os serviços da API.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>Coleção de serviços atualizada.</returns>
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddControllers();
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddProblemDetails();
        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();
        services.AddRouting(options => options.LowercaseUrls = true);
        services.AddAuthorization();
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("postgresql");

        AddAuthentication(services, configuration);
        AddSwagger(services);

        return services;
    }

    #region Helpers

    /// <summary>
    /// Operação para adicionar a autenticação JWT.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    private static void AddAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = GetJwtOptions(configuration);

        services.AddAuthentication(AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };
            });
    }

    /// <summary>
    /// Operação para obter as configurações de JWT.
    /// </summary>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>Configurações válidas de JWT.</returns>
    private static JwtOptions GetJwtOptions(IConfiguration configuration)
    {
        var jwtOptions = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()
            ?? throw new InvalidOperationException("As configurações de JWT não foram encontradas.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
            throw new InvalidOperationException("O emissor do JWT não foi configurado.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
            throw new InvalidOperationException("A audiência do JWT não foi configurada.");

        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
            throw new InvalidOperationException("A chave de assinatura do JWT não foi configurada.");

        if (jwtOptions.AccessTokenLifetimeMinutes <= 0)
            throw new InvalidOperationException("O tempo de vida do access token deve ser maior que zero.");

        if (jwtOptions.RefreshTokenLifetimeDays <= 0)
            throw new InvalidOperationException("O tempo de vida do refresh token deve ser maior que zero.");

        if (jwtOptions.ClockSkewSeconds < 0)
            throw new InvalidOperationException("A tolerância de clock do JWT não pode ser negativa.");

        return jwtOptions;
    }

    /// <summary>
    /// Operação para adicionar a configuração do Swagger.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    private static void AddSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            var documentationFileName = $"{typeof(ApiDependencyInjection).Assembly.GetName().Name}.xml";
            var documentationFilePath = Path.Combine(AppContext.BaseDirectory, documentationFileName);

            options.IncludeXmlComments(documentationFilePath, includeControllerXmlComments: true);
            options.AddSecurityDefinition(AuthenticationScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description = "Informe o token JWT no formato Bearer."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = AuthenticationScheme
                        }
                    },
                    []
                }
            });
        });
    }

    #endregion
}
