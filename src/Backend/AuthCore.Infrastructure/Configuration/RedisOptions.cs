using System.ComponentModel.DataAnnotations;

namespace AuthCore.Infrastructure.Configuration;

/// <summary>
/// Configuração de Redis usada para sessões e refresh tokens.
/// </summary>
public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    public string KeyPrefix { get; init; } = "authcore";
}
