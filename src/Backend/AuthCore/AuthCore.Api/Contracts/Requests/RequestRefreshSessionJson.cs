namespace AuthCore.Api.Contracts.Requests;

/// <summary>
/// Representa requisição para renovar uma sessão autenticada.
/// </summary>
public sealed class RequestRefreshSessionJson
{
    /// <summary>
    /// Refresh token informado pelo cliente.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
