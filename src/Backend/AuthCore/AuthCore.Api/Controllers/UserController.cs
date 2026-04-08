using AuthCore.Api.Contracts;
using AuthCore.Application.Common.Models.Responses;
using AuthCore.Application.Users.Models.Requests;
using AuthCore.Application.Users.Models.Responses;
using AuthCore.Application.Users.UseCases.ChangePassword;
using AuthCore.Application.Users.UseCases.Delete;
using AuthCore.Application.Users.UseCases.GetUserProfile;
using AuthCore.Application.Users.UseCases.Register;
using AuthCore.Application.Users.UseCases.Update;
using Microsoft.AspNetCore.Mvc;

namespace AuthCore.Api.Controllers;

/// <summary>
/// Representa controller responsável pelas operações de usuário.
/// </summary>
[ApiController]
[Route("api/users")]
public sealed class UserController : ControllerBase
{
    /// <summary>
    /// Operação para registrar um usuário.
    /// </summary>
    /// <param name="useCase">Caso de uso responsável pelo registro do usuário.</param>
    /// <param name="request">Dados da requisição de registro.</param>
    /// <returns>Resposta com os dados do usuário registrado.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ResponseRegisteredUserJson), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseRegisteredUserJson>> Register(
        [FromServices] IRegisterUserUseCase useCase,
        [FromBody] RequestRegisterUserJson request)
    {
        var result = await useCase.Execute(request);

        return Created(string.Empty, result);
    }

    /// <summary>
    /// Operação para obter o perfil do usuário autenticado.
    /// </summary>
    /// <param name="useCase">Caso de uso responsável por consultar o perfil do usuário.</param>
    /// <returns>Resposta com os dados do perfil do usuário.</returns>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ResponseUserProfileJson), StatusCodes.Status200OK)]
    [AuthenticatedUser]
    public async Task<ActionResult<ResponseUserProfileJson>> GetUserProfile(
        [FromServices] IGetUserProfileUseCase useCase)
    {
        var result = await useCase.Execute();

        return Ok(result);
    }

    /// <summary>
    /// Operação para atualizar o perfil do usuário autenticado.
    /// </summary>
    /// <param name="useCase">Caso de uso responsável pela atualização do usuário.</param>
    /// <param name="request">Dados da requisição de atualização.</param>
    /// <returns>Resposta sem conteúdo após a atualização do usuário.</returns>
    [HttpPut("profile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [AuthenticatedUser]
    public async Task<ActionResult> UpdateUserProfile(
        [FromServices] IUpdateUserUseCase useCase,
        [FromBody] RequestUpdateUserJson request)
    {
        await useCase.Execute(request);

        return NoContent();
    }

    /// <summary>
    /// Operação para alterar a senha do usuário autenticado.
    /// </summary>
    /// <param name="useCase">Caso de uso responsável pela alteração de senha.</param>
    /// <param name="request">Dados da requisição de alteração de senha.</param>
    /// <returns>Resposta sem conteúdo após a alteração da senha.</returns>
    [HttpPut("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [AuthenticatedUser]
    public async Task<ActionResult> ChangePassword(
        [FromServices] IChangePasswordUseCase useCase,
        [FromBody] RequestChangePasswordJson request)
    {
        await useCase.Execute(request);

        return NoContent();
    }

    /// <summary>
    /// Operação para excluir o usuário autenticado.
    /// </summary>
    /// <param name="useCase">Caso de uso responsável pela exclusão do usuário.</param>
    /// <returns>Resposta sem conteúdo após a exclusão do usuário.</returns>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [AuthenticatedUser]
    public async Task<ActionResult> Delete(
        [FromServices] IRequestDeleteUserUseCase useCase)
    {
        await useCase.Execute();

        return NoContent();
    }
}
