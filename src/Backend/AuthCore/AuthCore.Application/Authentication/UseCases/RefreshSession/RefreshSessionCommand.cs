namespace AuthCore.Application.Authentication.UseCases.RefreshSession;

/// <summary>
/// Representa comando para renovar uma sessão autenticada.
/// </summary>
public sealed class RefreshSessionCommand
{
    /// <summary>
    /// Refresh token informado pelo cliente.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
