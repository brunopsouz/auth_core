namespace AuthCore.Api.Contracts.Requests;

/// <summary>
/// Representa requisição para encerrar a autenticação do modo token.
/// </summary>
public sealed class RequestTokenLogoutJson
{
    /// <summary>
    /// Refresh token informado pelo cliente.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
