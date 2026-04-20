using System.Globalization;
using System.Security.Claims;
using AuthCore.Api.Authentication;
using AuthCore.Api.Contracts;
using AuthCore.Api.Contracts.Requests;
using AuthCore.Api.Contracts.Responses;
using AuthCore.Api.Security;
using AuthCore.Application.Authentication.Models;
using AuthCore.Application.Authentication.UseCases.GetUserSessions;
using AuthCore.Application.Authentication.UseCases.Login;
using AuthCore.Application.Authentication.UseCases.LoginSession;
using AuthCore.Application.Authentication.UseCases.LogoutAllSessions;
using AuthCore.Application.Authentication.UseCases.LogoutCurrentSession;
using AuthCore.Application.Authentication.UseCases.LogoutSession;
using AuthCore.Application.Authentication.UseCases.RefreshSession;
using AuthCore.Application.Authentication.UseCases.RevokeUserSession;
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
    private const string TOO_MANY_LOGIN_ATTEMPTS_MESSAGE = "Muitas tentativas de login. Aguarde alguns minutos e tente novamente.";

    private readonly ICsrfRequestValidator _csrfRequestValidator;
    private readonly ILoginRateLimiter _loginRateLimiter;
    private readonly ILogger<AuthController> _logger;

    #region Constructors

    /// <summary>
    /// Operação para criar instância da classe.
    /// </summary>
    /// <param name="csrfRequestValidator">Validador de origem das mutações autenticadas por cookie.</param>
    /// <param name="loginRateLimiter">Limitador de tentativas de login.</param>
    /// <param name="logger">Logger do fluxo de autenticação.</param>
    public AuthController(
        ICsrfRequestValidator csrfRequestValidator,
        ILoginRateLimiter loginRateLimiter,
        ILogger<AuthController> logger)
    {
        ArgumentNullException.ThrowIfNull(csrfRequestValidator);
        ArgumentNullException.ThrowIfNull(loginRateLimiter);
        ArgumentNullException.ThrowIfNull(logger);

        _csrfRequestValidator = csrfRequestValidator;
        _loginRateLimiter = loginRateLimiter;
        _logger = logger;
    }

    #endregion

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
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ResponseAuthenticatedUserJson>> Login(
        [FromServices] ILoginSessionUseCase useCase,
        [FromServices] IOptions<AuthCookieOptions> authCookieOptions,
        [FromBody] RequestSessionLoginJson request)
    {
        var rateLimitResult = await TryAcquireLoginRateLimitAsync(request.Email);

        if (rateLimitResult is not null)
            return rateLimitResult;

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
            _logger.LogInformation(
                "Login por sessão realizado com sucesso. UserId={UserId} Email={Email} IpAddress={IpAddress}",
                result.UserIdentifier,
                result.Email,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(CreateAuthenticatedUserResponse(result.UserIdentifier, result.Email));
        }
        catch (Exception exception) when (TryMapKnownException(exception, out var actionResult))
        {
            LogLoginFailure(exception, request.Email);
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
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ResponseAuthenticatedSessionJson>> Token(
        [FromServices] ILoginUseCase useCase,
        [FromBody] RequestLoginJson request)
    {
        var rateLimitResult = await TryAcquireLoginRateLimitAsync(request.Email);

        if (rateLimitResult is not null)
            return rateLimitResult;

        try
        {
            var result = await useCase.Execute(new LoginCommand
            {
                Email = request.Email,
                Password = request.Password
            });

            _logger.LogInformation(
                "Login no modo token realizado com sucesso. Email={Email} IpAddress={IpAddress}",
                request.Email,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(CreateAuthenticatedSessionResponse(result));
        }
        catch (Exception exception) when (TryMapKnownException(exception, out var actionResult))
        {
            LogLoginFailure(exception, request.Email);
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
    /// Operação para encerrar a autenticação do modo token.
    /// </summary>
    /// <param name="useCase">Caso de uso responsável por encerrar a autenticação do modo token.</param>
    /// <param name="request">Dados da requisição de logout token-based.</param>
    /// <returns>Resposta sem conteúdo após a revogação do refresh token informado.</returns>
    [HttpPost("token/logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> TokenLogout(
        [FromServices] ILogoutSessionUseCase useCase,
        [FromBody] RequestTokenLogoutJson request)
    {
        try
        {
            await useCase.Execute(new LogoutSessionCommand
            {
                RefreshToken = request.RefreshToken
            });

            _logger.LogInformation(
                "Logout do modo token concluído. IpAddress={IpAddress}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return NoContent();
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
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> Logout(
        [FromServices] ILogoutCurrentSessionUseCase useCase,
        [FromServices] IOptions<AuthCookieOptions> authCookieOptions)
    {
        try
        {
            _csrfRequestValidator.Validate(Request);
            var sessionId = GetAuthenticatedSessionId();

            await useCase.Execute(new LogoutCurrentSessionCommand
            {
                SessionId = sessionId
            });
            DeleteSessionCookie(authCookieOptions.Value);
            _logger.LogInformation(
                "Sessão atual encerrada. SessionId={SessionId}",
                sessionId);

            return NoContent();
        }
        catch (Exception exception) when (TryMapKnownException(exception, out var actionResult))
        {
            return actionResult;
        }
    }

    /// <summary>
    /// Operação para listar as sessões ativas do usuário autenticado.
    /// </summary>
    /// <param name="useCase">Caso de uso responsável pela listagem de sessões.</param>
    /// <returns>Resposta com as sessões ativas do usuário.</returns>
    [HttpGet("sessions")]
    [AuthenticatedSession]
    [ProducesResponseType(typeof(ResponseUserSessionsJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ResponseUserSessionsJson>> GetSessions(
        [FromServices] IGetUserSessionsUseCase useCase)
    {
        try
        {
            var result = await useCase.Execute(new GetUserSessionsQuery
            {
                UserId = GetAuthenticatedInternalUserId(),
                CurrentSessionId = GetAuthenticatedSessionId()
            });

            return Ok(CreateUserSessionsResponse(result));
        }
        catch (Exception exception) when (TryMapKnownException(exception, out var actionResult))
        {
            return actionResult;
        }
    }

    /// <summary>
    /// Operação para revogar uma sessão específica do usuário autenticado.
    /// </summary>
    /// <param name="sid">Identificador público da sessão alvo.</param>
    /// <param name="useCase">Caso de uso responsável pela revogação da sessão.</param>
    /// <param name="authCookieOptions">Configurações do cookie de autenticação.</param>
    /// <returns>Resposta sem conteúdo após a revogação da sessão.</returns>
    [HttpDelete("sessions/{sid}")]
    [AuthenticatedSession]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RevokeSession(
        [FromRoute] string sid,
        [FromServices] IRevokeUserSessionUseCase useCase,
        [FromServices] IOptions<AuthCookieOptions> authCookieOptions)
    {
        try
        {
            _csrfRequestValidator.Validate(Request);
            var normalizedSessionId = NormalizeSessionId(sid);
            var currentSessionId = GetAuthenticatedSessionId();
            var userId = GetAuthenticatedInternalUserId();

            await useCase.Execute(new RevokeUserSessionCommand
            {
                UserId = userId,
                SessionId = normalizedSessionId
            });

            if (string.Equals(currentSessionId, normalizedSessionId, StringComparison.Ordinal))
                DeleteSessionCookie(authCookieOptions.Value);

            _logger.LogInformation(
                "Sessão revogada pelo usuário autenticado. SessionId={SessionId} CurrentSessionId={CurrentSessionId} UserId={UserId}",
                normalizedSessionId,
                currentSessionId,
                userId);

            return NoContent();
        }
        catch (Exception exception) when (TryMapKnownException(exception, out var actionResult))
        {
            return actionResult;
        }
    }

    /// <summary>
    /// Operação para revogar todas as sessões do usuário autenticado.
    /// </summary>
    /// <param name="useCase">Caso de uso responsável pela revogação global das sessões.</param>
    /// <param name="authCookieOptions">Configurações do cookie de autenticação.</param>
    /// <returns>Resposta sem conteúdo após a revogação global.</returns>
    [HttpPost("logout-all")]
    [AuthenticatedSession]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> LogoutAll(
        [FromServices] ILogoutAllSessionsUseCase useCase,
        [FromServices] IOptions<AuthCookieOptions> authCookieOptions)
    {
        try
        {
            _csrfRequestValidator.Validate(Request);
            var userId = GetAuthenticatedInternalUserId();

            await useCase.Execute(new LogoutAllSessionsCommand
            {
                UserId = userId
            });
            DeleteSessionCookie(authCookieOptions.Value);
            _logger.LogInformation("Todas as sessões do usuário foram revogadas. UserId={UserId}", userId);

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
    /// Operação para criar a resposta HTTP da listagem de sessões do usuário.
    /// </summary>
    /// <param name="result">Resultado da listagem de sessões.</param>
    /// <returns>Resposta pronta para serialização HTTP.</returns>
    private static ResponseUserSessionsJson CreateUserSessionsResponse(UserSessionsResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new ResponseUserSessionsJson
        {
            CurrentSid = result.CurrentSessionId,
            Sessions = result.Sessions
                .Select(session => new ResponseUserSessionJson
                {
                    Sid = session.SessionId,
                    CreatedAtUtc = session.CreatedAtUtc,
                    LastSeenAtUtc = session.LastSeenAtUtc,
                    Ip = session.IpAddress,
                    UserAgent = session.UserAgent,
                    ExpiresAtUtc = session.ExpiresAtUtc
                })
                .ToArray()
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
    /// Operação para obter o identificador interno do usuário autenticado.
    /// </summary>
    /// <returns>Identificador interno do usuário autenticado.</returns>
    private Guid GetAuthenticatedInternalUserId()
    {
        var claimValue = User.FindFirstValue(SessionAuthenticationDefaults.InternalUserIdClaimType);

        if (Guid.TryParse(claimValue, out var userId))
            return userId;

        throw new UnauthorizedAccessException("O identificador interno do usuário autenticado não foi encontrado.");
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
        Response.Cookies.Append(
            authCookieOptions.SessionCookieName,
            sessionId,
            SessionCookiePolicy.CreateSessionCookie(authCookieOptions, expiresAtUtc));
    }

    /// <summary>
    /// Operação para remover o cookie da sessão autenticada.
    /// </summary>
    /// <param name="authCookieOptions">Configurações do cookie da sessão.</param>
    private void DeleteSessionCookie(AuthCookieOptions authCookieOptions)
    {
        Response.Cookies.Delete(
            authCookieOptions.SessionCookieName,
            SessionCookiePolicy.CreateExpiredSessionCookie(authCookieOptions));
    }

    /// <summary>
    /// Operação para tentar consumir uma cota do rate limit de login.
    /// </summary>
    /// <param name="requestEmail">E-mail informado no login.</param>
    /// <returns>Resultado HTTP de bloqueio quando o limite é excedido; caso contrário, nulo.</returns>
    private async Task<ActionResult?> TryAcquireLoginRateLimitAsync(string? requestEmail)
    {
        var result = await _loginRateLimiter.TryAcquireAsync(
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            requestEmail);

        if (result.IsAllowed)
        {
            return null;
        }

        var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(result.RetryAfter.TotalSeconds));

        Response.Headers["Retry-After"] = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        _logger.LogWarning(
            "Tentativa de login bloqueada por rate limit. Email={Email} IpAddress={IpAddress} RetryAfterSeconds={RetryAfterSeconds}",
            requestEmail,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            retryAfterSeconds);

        return StatusCode(
            StatusCodes.Status429TooManyRequests,
            CreateErrorResponse(TOO_MANY_LOGIN_ATTEMPTS_MESSAGE));
    }

    /// <summary>
    /// Operação para registrar falhas conhecidas de autenticação.
    /// </summary>
    /// <param name="exception">Exceção mapeada do fluxo de login.</param>
    /// <param name="email">E-mail informado na tentativa.</param>
    private void LogLoginFailure(Exception exception, string email)
    {
        if (exception is UnauthorizedAccessException or ForbiddenException)
        {
            _logger.LogWarning(
                exception,
                "Falha de login. Email={Email} IpAddress={IpAddress}",
                email,
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }
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

    /// <summary>
    /// Operação para normalizar o identificador da sessão informado pela rota.
    /// </summary>
    /// <param name="sid">Identificador informado.</param>
    /// <returns>Identificador normalizado.</returns>
    private static string NormalizeSessionId(string sid)
    {
        return string.IsNullOrWhiteSpace(sid)
            ? string.Empty
            : sid.Trim();
    }

    #endregion
}
