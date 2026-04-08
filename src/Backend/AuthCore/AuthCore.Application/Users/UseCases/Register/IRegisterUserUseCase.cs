using AuthCore.Application.Users.Models.Requests;
using AuthCore.Application.Users.Models.Responses;

namespace AuthCore.Application.Users.UseCases.Register;

/// <summary>
/// Define operação para registrar um usuário.
/// </summary>
public interface IRegisterUserUseCase
{
    /// <summary>
    /// Operação para registrar um usuário.
    /// </summary>
    /// <param name="request">Dados da requisição de registro.</param>
    /// <returns>Resposta com os dados do usuário registrado.</returns>
    Task<ResponseRegisteredUserJson> Execute(RequestRegisterUserJson request);
}
