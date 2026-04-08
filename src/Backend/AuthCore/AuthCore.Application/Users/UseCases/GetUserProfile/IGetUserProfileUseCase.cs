using AuthCore.Application.Users.Models.Responses;

namespace AuthCore.Application.Users.UseCases.GetUserProfile;

/// <summary>
/// Define operação para obter o perfil do usuário autenticado.
/// </summary>
public interface IGetUserProfileUseCase
{
    /// <summary>
    /// Operação para obter o perfil do usuário autenticado.
    /// </summary>
    /// <returns>Resposta com os dados do perfil do usuário.</returns>
    Task<ResponseUserProfileJson> Execute();
}
