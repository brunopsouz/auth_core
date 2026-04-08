using AuthCore.Application.Users.Models.Requests;

namespace AuthCore.Application.Users.UseCases.ChangePassword;

/// <summary>
/// Define operação para alterar a senha do usuário autenticado.
/// </summary>
public interface IChangePasswordUseCase
{
    /// <summary>
    /// Operação para alterar a senha do usuário autenticado.
    /// </summary>
    /// <param name="request">Dados da requisição de alteração de senha.</param>
    Task Execute(RequestChangePasswordJson request);
}
