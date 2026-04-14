namespace AuthCore.Domain.Security.Tokens.Models;

/// <summary>
/// Representa o resultado da emissão de um access token.
/// </summary>
public sealed class AccessTokenResult
{
    /// <summary>
    /// Token JWT emitido.
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// Identificador do token emitido.
    /// </summary>
    public Guid TokenId { get; init; }

    /// <summary>
    /// Data de expiração do token em UTC.
    /// </summary>
    public DateTime ExpiresAtUtc { get; init; }
}
