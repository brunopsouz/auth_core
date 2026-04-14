namespace AuthCore.Api.Contracts.Requests;

/// <summary>
/// Representa requisição para encerrar uma sessão autenticada.
/// </summary>
public sealed class RequestLogoutSessionJson
{
    /// <summary>
    /// Refresh token informado pelo cliente.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
