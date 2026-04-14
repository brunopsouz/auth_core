using AuthCore.Api.Contracts.Requests;
using AuthCore.Api.Contracts.Responses;
using AuthCore.Application.Authentication.Models;
using AuthCore.Application.Authentication.UseCases.Login;
using AuthCore.Application.Authentication.UseCases.LogoutSession;
using AuthCore.Application.Authentication.UseCases.RefreshSession;
using AuthCore.Application.Common.Models.Responses;
using AuthCore.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace AuthCore.Api.Controllers;

/// <summary>
/// Representa controller responsável pelas operações de autenticação.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    /// <summary>
    /// Operação para autenticar um usuário.
    /// </summary>
    /// <param name="useCase">Caso de uso responsável pela autenticação.</param>
    /// <param name="request">Dados da requisição de login.</param>
    /// <returns>Resposta com os dados da sessão autenticada.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ResponseAuthenticatedSessionJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResponseAuthenticatedSessionJson>> Login(
        [FromServices] ILoginUseCase useCase,
        [FromBody] RequestLoginJson request)
    {
        try
        {
            var result = await useCase.Execute(new LoginCommand
            {
                Email = request.Email,
                Password = request.Password
            });

            return Ok(CreateAuthenticatedSessionResponse(result));
        }
        catch (Exception exception) when (TryMapKnownException(exception, out var actionResult))
        {
            return actionResult;
        }
    }

    /// <summary>
    /// Operação para renovar uma sessão autenticada.
    /// </summary>
    /// <param name="useCase">Caso de uso responsável pela renovação da sessão.</param>
    /// <param name="request">Dados da requisição de renovação.</param>
    /// <returns>Resposta com os dados da sessão renovada.</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ResponseAuthenticatedSessionJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResponseAuthenticatedSessionJson>> Refresh(
        [FromServices] IRefreshSessionUseCase useCase,
        [FromBody] RequestRefreshSessionJson request)
    {
        try
        {
            var result = await useCase.Execute(new RefreshSessionCommand
            {
                RefreshToken = request.RefreshToken
            });

            return Ok(CreateAuthenticatedSessionResponse(result));
        }
        catch (Exception exception) when (TryMapKnownException(exception, out var actionResult))
        {
            return actionResult;
        }
    }

    /// <summary>
    /// Operação para encerrar uma sessão autenticada.
    /// </summary>
    /// <param name="useCase">Caso de uso responsável pelo encerramento da sessão.</param>
    /// <param name="request">Dados da requisição de logout.</param>
    /// <returns>Resposta sem conteúdo após o encerramento da sessão.</returns>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Logout(
        [FromServices] ILogoutSessionUseCase useCase,
        [FromBody] RequestLogoutSessionJson request)
    {
        try
        {
            await useCase.Execute(new LogoutSessionCommand
            {
                RefreshToken = request.RefreshToken
            });

            return NoContent();
        }
        catch (Exception exception) when (TryMapKnownException(exception, out var actionResult))
        {
            return actionResult;
        }
    }

    #region Helpers

    /// <summary>
    /// Operação para criar a resposta HTTP da sessão autenticada.
    /// </summary>
    /// <param name="result">Resultado da sessão autenticada.</param>
    /// <returns>Resposta pronta para serialização HTTP.</returns>
    private static ResponseAuthenticatedSessionJson CreateAuthenticatedSessionResponse(AuthenticatedSessionResult result)
    {
        return new ResponseAuthenticatedSessionJson
        {
            AccessToken = result.AccessToken,
            AccessTokenExpiresAtUtc = result.AccessTokenExpiresAtUtc,
            RefreshToken = result.RefreshToken,
            RefreshTokenExpiresAtUtc = result.RefreshTokenExpiresAtUtc
        };
    }

    /// <summary>
    /// Operação para mapear exceções conhecidas da autenticação.
    /// </summary>
    /// <param name="exception">Exceção capturada durante o processamento.</param>
    /// <param name="actionResult">Resultado HTTP correspondente à exceção.</param>
    /// <returns><c>true</c> quando a exceção foi mapeada; caso contrário, <c>false</c>.</returns>
    private static bool TryMapKnownException(Exception exception, out ActionResult actionResult)
    {
        actionResult = exception switch
        {
            ArgumentException argumentException => new BadRequestObjectResult(CreateErrorResponse(argumentException.Message)),
            UnauthorizedAccessException unauthorizedAccessException => new UnauthorizedObjectResult(CreateErrorResponse(unauthorizedAccessException.Message)),
            NotFoundException notFoundException => new NotFoundObjectResult(CreateErrorResponse(notFoundException.Message)),
            ConflictException conflictException => new ConflictObjectResult(CreateErrorResponse(conflictException.Message)),
            DomainException domainException => new BadRequestObjectResult(CreateErrorResponse(domainException.Message)),
            _ => null!
        };

        return actionResult is not null;
    }

    /// <summary>
    /// Operação para criar a resposta padronizada de erro.
    /// </summary>
    /// <param name="errorMessage">Mensagem de erro da operação.</param>
    /// <returns>Resposta de erro padronizada.</returns>
    private static ResponseErrorJson CreateErrorResponse(string errorMessage)
    {
        return new ResponseErrorJson
        {
            Errors = [errorMessage]
        };
    }

    #endregion
}
