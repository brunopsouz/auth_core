using System.Security.Claims;
using AuthCore.Api.Authentication;
using AuthCore.Api.Contracts;
using AuthCore.Api.Contracts.Requests;
using AuthCore.Api.Contracts.Responses;
using AuthCore.Application.Authentication.Models;
using AuthCore.Application.Authentication.UseCases.Login;
using AuthCore.Application.Authentication.UseCases.LoginSession;
using AuthCore.Application.Authentication.UseCases.LogoutCurrentSession;
using AuthCore.Application.Authentication.UseCases.RefreshSession;
using AuthCore.Application.Common.Models.Responses;
using AuthCore.Domain.Common.Exceptions;
using AuthCore.Domain.Users.Enums;
using AuthCore.Infrastructure.Configurations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AuthCore.Api.Controllers;

/// <summary>
/// Representa controller responsável pelas operações de autenticação.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    /// <summary>
    /// Operação para autenticar um usuário por sessão.
    /// </summary>
    /// <param name="useCase">Caso de uso responsável pela autenticação por sessão.</param>
    /// <param name="authCookieOptions">Configurações do cookie de autenticação.</param>
    /// <param name="request">Dados da requisição de login.</param>
    /// <returns>Resposta com os dados do usuário autenticado.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ResponseAuthenticatedUserJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResponseAuthenticatedUserJson>> Login(
        [FromServices] ILoginSessionUseCase useCase,
        [FromServices] IOptions<AuthCookieOptions> authCookieOptions,
        [FromBody] RequestSessionLoginJson request)
    {
        try
        {
            var result = await useCase.Execute(new LoginSessionCommand
            {
                Email = request.Email,
                Password = request.Password,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            });

            AppendSessionCookie(result.SessionId, result.ExpiresAtUtc, authCookieOptions.Value);

            return Ok(CreateAuthenticatedUserResponse(result.UserIdentifier, result.Email));
        }
        catch (Exception exception) when (TryMapKnownException(exception, out var actionResult))
        {
            return actionResult;
        }
    }

    /// <summary>
    /// Operação para autenticar um usuário no modo token.
    /// </summary>
    /// <param name="useCase">Caso de uso responsável pela autenticação token-based.</param>
    /// <param name="request">Dados da requisição de login.</param>
    /// <returns>Resposta com os dados da sessão autenticada por token.</returns>
    [HttpPost("token")]
    [ProducesResponseType(typeof(ResponseAuthenticatedSessionJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResponseAuthenticatedSessionJson>> Token(
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
    /// Operação para renovar uma sessão autenticada por token.
    /// </summary>
    /// <param name="useCase">Caso de uso responsável pela renovação da sessão.</param>
    /// <param name="request">Dados da requisição de renovação.</param>
    /// <returns>Resposta com os dados da sessão renovada.</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ResponseAuthenticatedSessionJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status403Forbidden)]
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
    /// Operação para obter o usuário autenticado da sessão atual.
    /// </summary>
    /// <returns>Resposta com os dados do usuário autenticado.</returns>
    [HttpGet("me")]
    [AuthenticatedSession]
    [ProducesResponseType(typeof(ResponseAuthenticatedUserJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status403Forbidden)]
    public ActionResult<ResponseAuthenticatedUserJson> Me()
    {
        try
        {
            EnsureAuthenticatedSessionAllowsAccess();

            return Ok(CreateAuthenticatedUserResponse(
                GetAuthenticatedUserIdentifier(),
                GetAuthenticatedEmail()));
        }
        catch (Exception exception) when (TryMapKnownException(exception, out var actionResult))
        {
            return actionResult;
        }
    }

    /// <summary>
    /// Operação para encerrar a sessão autenticada atual.
    /// </summary>
    /// <param name="useCase">Caso de uso responsável pelo encerramento da sessão atual.</param>
    /// <param name="authCookieOptions">Configurações do cookie de autenticação.</param>
    /// <returns>Resposta sem conteúdo após o encerramento da sessão.</returns>
    [HttpPost("logout")]
    [AuthenticatedSession]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Logout(
        [FromServices] ILogoutCurrentSessionUseCase useCase,
        [FromServices] IOptions<AuthCookieOptions> authCookieOptions)
    {
        try
        {
            await useCase.Execute(new LogoutCurrentSessionCommand
            {
                SessionId = GetAuthenticatedSessionId()
            });
            DeleteSessionCookie(authCookieOptions.Value);

            return NoContent();
        }
        catch (Exception exception) when (TryMapKnownException(exception, out var actionResult))
        {
            return actionResult;
        }
    }

    #region Helpers

    /// <summary>
    /// Operação para criar a resposta HTTP do usuário autenticado.
    /// </summary>
    /// <param name="userIdentifier">Identificador público do usuário.</param>
    /// <param name="email">E-mail autenticado.</param>
    /// <returns>Resposta pronta para serialização HTTP.</returns>
    private static ResponseAuthenticatedUserJson CreateAuthenticatedUserResponse(Guid userIdentifier, string email)
    {
        return new ResponseAuthenticatedUserJson
        {
            UserId = userIdentifier,
            Email = email
        };
    }

    /// <summary>
    /// Operação para criar a resposta HTTP da sessão autenticada por token.
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

    /// <summary>
    /// Operação para obter o e-mail do usuário autenticado.
    /// </summary>
    /// <returns>E-mail do usuário autenticado.</returns>
    private string GetAuthenticatedEmail()
    {
        var claimValues = new[]
        {
            User.FindFirstValue(ClaimTypes.Email),
            User.FindFirstValue("email")
        };

        foreach (var claimValue in claimValues)
        {
            if (!string.IsNullOrWhiteSpace(claimValue))
                return claimValue;
        }

        throw new UnauthorizedAccessException("O e-mail do usuário autenticado não foi encontrado.");
    }

    /// <summary>
    /// Operação para obter o identificador da sessão autenticada.
    /// </summary>
    /// <returns>Identificador público da sessão autenticada.</returns>
    private string GetAuthenticatedSessionId()
    {
        var sessionId = User.FindFirstValue(SessionAuthenticationDefaults.SessionIdClaimType)
            ?? User.FindFirstValue("sid");

        if (!string.IsNullOrWhiteSpace(sessionId))
            return sessionId;

        throw new UnauthorizedAccessException("O identificador da sessão autenticada não foi encontrado.");
    }

    /// <summary>
    /// Operação para validar se a sessão autenticada pode acessar o recurso atual.
    /// </summary>
    private void EnsureAuthenticatedSessionAllowsAccess()
    {
        var userStatus = GetAuthenticatedUserStatus();
        var userIsActive = GetAuthenticatedUserIsActive();

        if (!userIsActive)
            throw new ForbiddenException("O usuário não pode autenticar no momento.");

        if (userStatus == UserStatus.PendingEmailVerification)
            throw new ForbiddenException("O usuário precisa verificar o e-mail antes de autenticar.");

        if (userStatus == UserStatus.Blocked)
            throw new ForbiddenException("O usuário está bloqueado para autenticação.");
    }

    /// <summary>
    /// Operação para obter o status funcional do usuário autenticado.
    /// </summary>
    /// <returns>Status funcional do usuário autenticado.</returns>
    private UserStatus GetAuthenticatedUserStatus()
    {
        var claimValue = User.FindFirstValue(SessionAuthenticationDefaults.UserStatusClaimType);

        if (Enum.TryParse<UserStatus>(claimValue, out var userStatus))
            return userStatus;

        throw new UnauthorizedAccessException("O status do usuário autenticado não foi encontrado.");
    }

    /// <summary>
    /// Operação para obter a indicação de atividade do usuário autenticado.
    /// </summary>
    /// <returns><c>true</c> quando o usuário está ativo; caso contrário, <c>false</c>.</returns>
    private bool GetAuthenticatedUserIsActive()
    {
        var claimValue = User.FindFirstValue(SessionAuthenticationDefaults.UserIsActiveClaimType);

        if (bool.TryParse(claimValue, out var userIsActive))
            return userIsActive;

        throw new UnauthorizedAccessException("O estado de atividade do usuário autenticado não foi encontrado.");
    }

    /// <summary>
    /// Operação para emitir o cookie da sessão autenticada.
    /// </summary>
    /// <param name="sessionId">Identificador público da sessão.</param>
    /// <param name="expiresAtUtc">Expiração da sessão em UTC.</param>
    /// <param name="authCookieOptions">Configurações do cookie da sessão.</param>
    private void AppendSessionCookie(string sessionId, DateTime expiresAtUtc, AuthCookieOptions authCookieOptions)
    {
        Response.Cookies.Append(authCookieOptions.SessionCookieName, sessionId, new CookieOptions
        {
            HttpOnly = true,
            Secure = authCookieOptions.Secure,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = new DateTimeOffset(expiresAtUtc)
        });
    }

    /// <summary>
    /// Operação para remover o cookie da sessão autenticada.
    /// </summary>
    /// <param name="authCookieOptions">Configurações do cookie da sessão.</param>
    private void DeleteSessionCookie(AuthCookieOptions authCookieOptions)
    {
        Response.Cookies.Delete(authCookieOptions.SessionCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = authCookieOptions.Secure,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
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
            ForbiddenException forbiddenException => new ObjectResult(CreateErrorResponse(forbiddenException.Message))
            {
                StatusCode = StatusCodes.Status403Forbidden
            },
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
