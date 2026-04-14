namespace AuthCore.Application.Authentication.UseCases.LogoutSession;

/// <summary>
/// Define operação para encerrar uma sessão autenticada.
/// </summary>
public interface ILogoutSessionUseCase
{
    /// <summary>
    /// Operação para encerrar uma sessão autenticada.
    /// </summary>
    /// <param name="command">Comando com o refresh token informado.</param>
    Task Execute(LogoutSessionCommand command);
}
