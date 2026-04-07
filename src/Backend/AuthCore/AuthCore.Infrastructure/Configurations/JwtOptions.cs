using System.ComponentModel.DataAnnotations;

namespace AuthCore.Infrastructure.Configurations;

/// <summary>
/// Representa as configurações de emissão do token JWT.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// Nome da seção de configuração.
    /// </summary>
    public const string SectionName = "Authentication:Jwt";

    /// <summary>
    /// Emissor do token.
    /// </summary>
    [Required]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Audiência do token.
    /// </summary>
    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Chave usada na assinatura do token.
    /// </summary>
    [Required]
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>
    /// Tempo de vida do access token em minutos.
    /// </summary>
    [Range(1, 1440)]
    public int AccessTokenLifetimeMinutes { get; init; } = 15;
}
