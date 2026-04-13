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
using System.Security.Claims;

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
        var command = new RegisterUserCommand
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Contact = request.Contact,
            Password = request.Password,
            ConfirmPassword = request.ConfirmPassword
        };

        var result = await useCase.Execute(command);
        var response = new ResponseRegisteredUserJson
        {
            UserIdentifier = result.UserIdentifier,
            FullName = result.FullName,
            Email = result.Email
        };

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
        var result = await useCase.Execute(new GetUserProfileQuery
        {
            UserIdentifier = GetAuthenticatedUserIdentifier()
        });
        var response = new ResponseUserProfileJson
        {
            FirstName = result.FirstName,
            LastName = result.LastName,
            FullName = result.FullName,
            Email = result.Email,
            Contact = result.Contact,
            Role = result.Role,
            IsEmailVerified = result.IsEmailVerified
        };

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
        var command = new UpdateUserCommand
        {
            UserIdentifier = GetAuthenticatedUserIdentifier(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Contact = request.Contact
        };

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
        var command = new ChangePasswordCommand
        {
            UserIdentifier = GetAuthenticatedUserIdentifier(),
            CurrentPassword = request.CurrentPassword,
            NewPassword = request.NewPassword,
            ConfirmNewPassword = request.ConfirmNewPassword
        };

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
        await useCase.Execute(new DeleteUserCommand
        {
            UserIdentifier = GetAuthenticatedUserIdentifier()
        });

        return NoContent();
    }

    #region Helpers

    /// <summary>
    /// Operação para obter o identificador público do usuário autenticado.
    /// </summary>
    /// <returns>Identificador público do usuário autenticado.</returns>
    private Guid GetAuthenticatedUserIdentifier()
    {
        var claimValues = new[]
        {
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.FindFirstValue("sub"),
            User.FindFirstValue("user_identifier"),
            User.FindFirstValue("userIdentifier")
        };

        foreach (var claimValue in claimValues)
        {
            if (Guid.TryParse(claimValue, out var userIdentifier))
                return userIdentifier;
        }

        throw new UnauthorizedAccessException("O identificador do usuário autenticado não foi encontrado.");
    }

    #endregion
}
