namespace AuthCore.Application.Authentication.UseCases.LogoutSession;

/// <summary>
/// Representa comando para encerrar uma sessão autenticada.
/// </summary>
public sealed class LogoutSessionCommand
{
    /// <summary>
    /// Refresh token informado pelo cliente.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
