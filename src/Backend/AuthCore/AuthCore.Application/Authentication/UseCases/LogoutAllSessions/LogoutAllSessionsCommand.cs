namespace AuthCore.Application.Authentication.UseCases.LogoutAllSessions;

/// <summary>
/// Representa o comando para revogar todas as sessões do usuário.
/// </summary>
public sealed class LogoutAllSessionsCommand
{
    /// <summary>
    /// Identificador interno do usuário autenticado.
    /// </summary>
    public Guid UserId { get; init; }
}
