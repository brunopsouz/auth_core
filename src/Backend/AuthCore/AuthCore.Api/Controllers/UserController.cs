using AuthCore.Api.Contracts;
using AuthCore.Api.Contracts.Requests;
using AuthCore.Api.Contracts.Responses;
using AuthCore.Application.Common.Models.Responses;
using AuthCore.Application.Users.UseCases.ChangePassword;
using AuthCore.Application.Users.UseCases.DeleteUser;
using AuthCore.Application.Users.UseCases.GetUserProfile;
using AuthCore.Application.Users.UseCases.RegisterUser;
using AuthCore.Application.Users.UseCases.UpdateUser;
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
        var command = CreateRegisterUserCommand(request);
        var result = await useCase.Execute(command);
        var response = CreateRegisteredUserResponse(result);

        return Created(string.Empty, response);
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
        var result = await useCase.Execute(new GetUserProfileQuery());
        var response = CreateUserProfileResponse(result);

        return Ok(response);
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
        var command = CreateUpdateUserCommand(request);

        await useCase.Execute(command);

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
        var command = CreateChangePasswordCommand(request);

        await useCase.Execute(command);

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
        [FromServices] IDeleteUserUseCase useCase)
    {
        await useCase.Execute(new DeleteUserCommand());

        return NoContent();
    }

    #region Helpers

    /// <summary>
    /// Operação para criar o comando de registro do usuário.
    /// </summary>
    /// <param name="request">Dados da requisição de registro.</param>
    /// <returns>Comando com os dados do registro.</returns>
    private static RegisterUserCommand CreateRegisterUserCommand(RequestRegisterUserJson request)
    {
        return new RegisterUserCommand
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Contact = request.Contact,
            Password = request.Password,
            ConfirmPassword = request.ConfirmPassword
        };
    }

    /// <summary>
    /// Operação para criar a resposta do usuário registrado.
    /// </summary>
    /// <param name="result">Resultado da aplicação.</param>
    /// <returns>Resposta HTTP do usuário registrado.</returns>
    private static ResponseRegisteredUserJson CreateRegisteredUserResponse(RegisterUserResult result)
    {
        return new ResponseRegisteredUserJson
        {
            UserIdentifier = result.UserIdentifier,
            FullName = result.FullName,
            Email = result.Email
        };
    }

    /// <summary>
    /// Operação para criar o comando de atualização do usuário.
    /// </summary>
    /// <param name="request">Dados da requisição de atualização.</param>
    /// <returns>Comando com os dados da atualização.</returns>
    private static UpdateUserCommand CreateUpdateUserCommand(RequestUpdateUserJson request)
    {
        return new UpdateUserCommand
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Contact = request.Contact
        };
    }

    /// <summary>
    /// Operação para criar o comando de alteração de senha.
    /// </summary>
    /// <param name="request">Dados da requisição de alteração de senha.</param>
    /// <returns>Comando com os dados da alteração de senha.</returns>
    private static ChangePasswordCommand CreateChangePasswordCommand(RequestChangePasswordJson request)
    {
        return new ChangePasswordCommand
        {
            CurrentPassword = request.CurrentPassword,
            NewPassword = request.NewPassword,
            ConfirmNewPassword = request.ConfirmNewPassword
        };
    }

    /// <summary>
    /// Operação para criar a resposta do perfil do usuário.
    /// </summary>
    /// <param name="result">Resultado da aplicação.</param>
    /// <returns>Resposta HTTP do perfil do usuário.</returns>
    private static ResponseUserProfileJson CreateUserProfileResponse(GetUserProfileResult result)
    {
        return new ResponseUserProfileJson
        {
            FirstName = result.FirstName,
            LastName = result.LastName,
            FullName = result.FullName,
            Email = result.Email,
            Contact = result.Contact,
            Role = result.Role,
            IsEmailVerified = result.IsEmailVerified
        };
    }

    #endregion
}
