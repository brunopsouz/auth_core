using AuthCore.Application.Users.Models.Requests;

namespace AuthCore.Application.Users.UseCases.Update;

/// <summary>
/// Define operação para atualizar o perfil do usuário autenticado.
/// </summary>
public interface IUpdateUserUseCase
{
    /// <summary>
    /// Operação para atualizar o perfil do usuário autenticado.
    /// </summary>
    /// <param name="request">Dados da requisição de atualização.</param>
    Task Execute(RequestUpdateUserJson request);
}
