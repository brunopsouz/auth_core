using AuthCore.Application.Authentication.Models;

namespace AuthCore.Application.Authentication.UseCases.RefreshSession;

/// <summary>
/// Define operação para renovar uma sessão autenticada.
/// </summary>
public interface IRefreshSessionUseCase
{
    /// <summary>
    /// Operação para renovar uma sessão autenticada.
    /// </summary>
    /// <param name="command">Comando com o refresh token informado.</param>
    /// <returns>Resultado da sessão renovada.</returns>
    Task<AuthenticatedSessionResult> Execute(RefreshSessionCommand command);
}
