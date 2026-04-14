using AuthCore.Application.Authentication.Models;

namespace AuthCore.Application.Authentication.UseCases.Login;

/// <summary>
/// Define operação para autenticar um usuário.
/// </summary>
public interface ILoginUseCase
{
    /// <summary>
    /// Operação para autenticar um usuário.
    /// </summary>
    /// <param name="command">Comando com as credenciais do login.</param>
    /// <returns>Resultado da sessão autenticada.</returns>
    Task<AuthenticatedSessionResult> Execute(LoginCommand command);
}
