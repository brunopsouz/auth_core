namespace AuthCore.Api.Contracts.Responses;

/// <summary>
/// Representa resposta de uma sessão autenticada.
/// </summary>
public sealed class ResponseAuthenticatedSessionJson
{
    /// <summary>
    /// Access token JWT emitido.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Data de expiração do access token em UTC.
    /// </summary>
    public DateTime AccessTokenExpiresAtUtc { get; set; }

    /// <summary>
    /// Refresh token emitido em texto puro.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Data de expiração do refresh token em UTC.
    /// </summary>
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
}
