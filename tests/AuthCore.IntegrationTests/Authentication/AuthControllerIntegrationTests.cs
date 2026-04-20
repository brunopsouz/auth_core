using System.Security.Claims;
using AuthCore.Api;
using AuthCore.Api.Authentication;
using AuthCore.Api.Contracts.Requests;
using AuthCore.Api.Contracts.Responses;
using AuthCore.Api.Controllers;
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
using AuthCore.Infrastructure.Configurations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AuthCore.IntegrationTests.Authentication;

/// <summary>
/// Verifica a exposição e o comportamento HTTP do controller de autenticação.
/// </summary>
public sealed class AuthControllerIntegrationTests
{
    [Fact]
    public async Task Login_WhenSessionUseCaseSucceeds_ShouldReturnOkWithCookieAndAuthenticatedUserResponse()
    {
        var userId = Guid.NewGuid();
        var useCase = new SpyLoginSessionUseCase
        {
            Result = new AuthenticatedUserSessionResult
            {
                SessionId = "session-123",
                UserIdentifier = userId,
                Email = "bruno@authcore.dev",
                ExpiresAtUtc = new DateTime(2026, 4, 20, 15, 0, 0, DateTimeKind.Utc)
            }
        };
        var authCookieOptions = Options.Create(new AuthCookieOptions
        {
            SessionCookieName = "sid",
            Secure = false
        });
        var controller = CreateController();

        var result = await controller.Login(useCase, authCookieOptions, new RequestSessionLoginJson
        {
            Email = "bruno@authcore.dev",
            Password = "ValidPassword#2026"
        });

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ResponseAuthenticatedUserJson>(okResult.Value);
        var setCookieHeader = controller.Response.Headers.SetCookie.ToString();

        Assert.Equal("bruno@authcore.dev", useCase.LastCommand!.Email);
        Assert.Equal("ValidPassword#2026", useCase.LastCommand.Password);
        Assert.Equal(userId, response.UserId);
        Assert.Equal(useCase.Result.Email, response.Email);
        Assert.Contains("sid=session-123", setCookieHeader, StringComparison.Ordinal);
        Assert.Contains("httponly", setCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", setCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/", setCookieHeader, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WhenUseCaseThrowsForbiddenException_ShouldReturnForbiddenResponseErrorJson()
    {
        var useCase = new ThrowingLoginSessionUseCase(new ForbiddenException("O usuário precisa verificar o e-mail antes de autenticar."));
        var controller = CreateController();

        var result = await controller.Login(useCase, Options.Create(new AuthCookieOptions()), new RequestSessionLoginJson
        {
            Email = "pending@authcore.dev",
            Password = "ValidPassword#2026"
        });

        var forbiddenResult = Assert.IsType<ObjectResult>(result.Result);
        var response = Assert.IsType<ResponseErrorJson>(forbiddenResult.Value);

        Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
        Assert.Equal(["O usuário precisa verificar o e-mail antes de autenticar."], response.Errors);
    }

    [Fact]
    public async Task Token_WhenUseCaseSucceeds_ShouldReturnOkWithAuthenticatedSessionResponse()
    {
        var useCase = new SpyLoginUseCase
        {
            Result = new AuthenticatedSessionResult
            {
                AccessToken = "access-token",
                AccessTokenExpiresAtUtc = new DateTime(2026, 4, 13, 15, 0, 0, DateTimeKind.Utc),
                RefreshToken = "refresh-token",
                RefreshTokenExpiresAtUtc = new DateTime(2026, 4, 20, 15, 0, 0, DateTimeKind.Utc)
            }
        };
        var controller = CreateController();

        var result = await controller.Token(useCase, new RequestLoginJson
        {
            Email = "bruno@authcore.dev",
            Password = "ValidPassword#2026"
        });

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ResponseAuthenticatedSessionJson>(okResult.Value);

        Assert.Equal("bruno@authcore.dev", useCase.LastCommand!.Email);
        Assert.Equal("ValidPassword#2026", useCase.LastCommand.Password);
        Assert.Equal(useCase.Result.AccessToken, response.AccessToken);
        Assert.Equal(useCase.Result.RefreshToken, response.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WhenUseCaseSucceeds_ShouldReturnOkWithAuthenticatedSessionResponse()
    {
        var useCase = new SpyRefreshSessionUseCase
        {
            Result = new AuthenticatedSessionResult
            {
                AccessToken = "next-access-token",
                AccessTokenExpiresAtUtc = new DateTime(2026, 4, 13, 15, 15, 0, DateTimeKind.Utc),
                RefreshToken = "next-refresh-token",
                RefreshTokenExpiresAtUtc = new DateTime(2026, 4, 20, 15, 15, 0, DateTimeKind.Utc)
            }
        };
        var controller = CreateController();

        var result = await controller.Refresh(useCase, new RequestRefreshSessionJson
        {
            RefreshToken = "current-refresh-token"
        });

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ResponseAuthenticatedSessionJson>(okResult.Value);

        Assert.Equal("current-refresh-token", useCase.LastCommand!.RefreshToken);
        Assert.Equal(useCase.Result.AccessToken, response.AccessToken);
        Assert.Equal(useCase.Result.RefreshToken, response.RefreshToken);
    }

    [Fact]
    public async Task TokenLogout_WhenUseCaseSucceeds_ShouldReturnNoContentAndForwardRefreshToken()
    {
        var useCase = new SpyLogoutSessionUseCase();
        var controller = CreateController();

        var result = await controller.TokenLogout(useCase, new RequestTokenLogoutJson
        {
            RefreshToken = "refresh-token"
        });

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("refresh-token", useCase.LastCommand!.RefreshToken);
    }

    [Fact]
    public async Task TokenLogout_WhenUseCaseThrowsUnauthorizedAccessException_ShouldReturnUnauthorizedResponseErrorJson()
    {
        var useCase = new ThrowingLogoutSessionUseCase(new UnauthorizedAccessException("A sessão informada é inválida ou expirou."));
        var controller = CreateController();

        var result = await controller.TokenLogout(useCase, new RequestTokenLogoutJson
        {
            RefreshToken = "invalid-refresh-token"
        });

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ResponseErrorJson>(unauthorizedResult.Value);

        Assert.Equal(["A sessão informada é inválida ou expirou."], response.Errors);
    }

    [Fact]
    public async Task TokenLogout_WhenUseCaseCompletesForRepeatedLogout_ShouldRemainNoContent()
    {
        var useCase = new SpyLogoutSessionUseCase();
        var controller = CreateController();

        _ = await controller.TokenLogout(useCase, new RequestTokenLogoutJson
        {
            RefreshToken = "refresh-token"
        });

        var result = await controller.TokenLogout(useCase, new RequestTokenLogoutJson
        {
            RefreshToken = "refresh-token"
        });

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("refresh-token", useCase.LastCommand!.RefreshToken);
    }

    [Fact]
    public void Me_WhenUserClaimsArePresent_ShouldReturnOkWithAuthenticatedUserResponse()
    {
        var userId = Guid.NewGuid();
        var controller = CreateController(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "bruno@authcore.dev"),
            new Claim(SessionAuthenticationDefaults.SessionIdClaimType, "session-123"),
            new Claim(SessionAuthenticationDefaults.UserStatusClaimType, "Active"),
            new Claim(SessionAuthenticationDefaults.UserIsActiveClaimType, bool.TrueString)
        });

        var result = controller.Me();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ResponseAuthenticatedUserJson>(okResult.Value);

        Assert.Equal(userId, response.UserId);
        Assert.Equal("bruno@authcore.dev", response.Email);
    }

    [Fact]
    public void Me_WhenSessionUserIsPending_ShouldReturnForbiddenResponseErrorJson()
    {
        var controller = CreateController(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, "pending@authcore.dev"),
            new Claim(SessionAuthenticationDefaults.SessionIdClaimType, "session-123"),
            new Claim(SessionAuthenticationDefaults.UserStatusClaimType, "PendingEmailVerification"),
            new Claim(SessionAuthenticationDefaults.UserIsActiveClaimType, bool.TrueString)
        });

        var result = controller.Me();

        var forbiddenResult = Assert.IsType<ObjectResult>(result.Result);
        var response = Assert.IsType<ResponseErrorJson>(forbiddenResult.Value);

        Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
        Assert.Equal(["O usuário precisa verificar o e-mail antes de autenticar."], response.Errors);
    }

    [Fact]
    public async Task Logout_WhenUseCaseSucceeds_ShouldReturnNoContentAndDeleteCookie()
    {
        var useCase = new SpyLogoutCurrentSessionUseCase();
        var authCookieOptions = Options.Create(new AuthCookieOptions
        {
            SessionCookieName = "sid",
            Secure = false
        });
        var controller = CreateController(new[]
        {
            new Claim(SessionAuthenticationDefaults.SessionIdClaimType, "session-123")
        });

        var result = await controller.Logout(useCase, authCookieOptions);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("session-123", useCase.LastCommand!.SessionId);
        Assert.Contains("sid=", controller.Response.Headers.SetCookie.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Logout_WhenOriginIsNotAllowed_ShouldReturnForbiddenResponseErrorJson()
    {
        var useCase = new SpyLogoutCurrentSessionUseCase();
        var authCookieOptions = Options.Create(new AuthCookieOptions
        {
            SessionCookieName = "sid",
            Secure = false
        });
        var controller = CreateController(
            [
                new Claim(SessionAuthenticationDefaults.SessionIdClaimType, "session-123"),
                new Claim(SessionAuthenticationDefaults.InternalUserIdClaimType, Guid.NewGuid().ToString())
            ],
            csrfRequestValidator: CreateCsrfRequestValidator("https://app.authcore.dev"));

        controller.Request.Headers.Origin = "https://evil.authcore.dev";

        var result = await controller.Logout(useCase, authCookieOptions);

        var forbiddenResult = Assert.IsType<ObjectResult>(result);
        var response = Assert.IsType<ResponseErrorJson>(forbiddenResult.Value);

        Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
        Assert.Equal(["A origem da requisição não é permitida."], response.Errors);
        Assert.Null(useCase.LastCommand);
    }

    [Fact]
    public async Task GetSessions_WhenUseCaseSucceeds_ShouldReturnOkWithSessionsResponse()
    {
        var userId = Guid.NewGuid();
        var useCase = new SpyGetUserSessionsUseCase
        {
            Result = new UserSessionsResult
            {
                CurrentSessionId = "session-123",
                Sessions =
                [
                    new UserSessionResult
                    {
                        SessionId = "session-123",
                        CreatedAtUtc = new DateTime(2026, 4, 18, 10, 0, 0, DateTimeKind.Utc),
                        LastSeenAtUtc = new DateTime(2026, 4, 18, 10, 30, 0, DateTimeKind.Utc),
                        IpAddress = "127.0.0.1",
                        UserAgent = "Browser A",
                        ExpiresAtUtc = new DateTime(2026, 4, 18, 12, 0, 0, DateTimeKind.Utc)
                    }
                ]
            }
        };
        var controller = CreateController(new[]
        {
            new Claim(SessionAuthenticationDefaults.InternalUserIdClaimType, userId.ToString()),
            new Claim(SessionAuthenticationDefaults.SessionIdClaimType, "session-123")
        });

        var result = await controller.GetSessions(useCase);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ResponseUserSessionsJson>(okResult.Value);
        var session = Assert.Single(response.Sessions);

        Assert.Equal(userId, useCase.LastQuery!.UserId);
        Assert.Equal("session-123", useCase.LastQuery.CurrentSessionId);
        Assert.Equal("session-123", response.CurrentSid);
        Assert.Equal("session-123", session.Sid);
        Assert.Equal("127.0.0.1", session.Ip);
    }

    [Fact]
    public async Task RevokeSession_WhenRevokingCurrentSession_ShouldReturnNoContentAndDeleteCookie()
    {
        var userId = Guid.NewGuid();
        var useCase = new SpyRevokeUserSessionUseCase();
        var authCookieOptions = Options.Create(new AuthCookieOptions
        {
            SessionCookieName = "sid",
            Secure = false
        });
        var controller = CreateController(new[]
        {
            new Claim(SessionAuthenticationDefaults.InternalUserIdClaimType, userId.ToString()),
            new Claim(SessionAuthenticationDefaults.SessionIdClaimType, "session-123")
        });

        var result = await controller.RevokeSession("session-123", useCase, authCookieOptions);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(userId, useCase.LastCommand!.UserId);
        Assert.Equal("session-123", useCase.LastCommand.SessionId);
        Assert.Contains("sid=", controller.Response.Headers.SetCookie.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RevokeSession_WhenRevokingCurrentSessionWithWhitespace_ShouldNormalizeSessionIdAndDeleteCookie()
    {
        var userId = Guid.NewGuid();
        var useCase = new SpyRevokeUserSessionUseCase();
        var authCookieOptions = Options.Create(new AuthCookieOptions
        {
            SessionCookieName = "sid",
            Secure = false
        });
        var controller = CreateController(new[]
        {
            new Claim(SessionAuthenticationDefaults.InternalUserIdClaimType, userId.ToString()),
            new Claim(SessionAuthenticationDefaults.SessionIdClaimType, "session-123")
        });

        var result = await controller.RevokeSession("  session-123  ", useCase, authCookieOptions);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("session-123", useCase.LastCommand!.SessionId);
        Assert.Contains("sid=", controller.Response.Headers.SetCookie.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RevokeSession_WhenRefererIsAllowed_ShouldReturnNoContent()
    {
        var userId = Guid.NewGuid();
        var useCase = new SpyRevokeUserSessionUseCase();
        var authCookieOptions = Options.Create(new AuthCookieOptions
        {
            SessionCookieName = "sid",
            Secure = false
        });
        var controller = CreateController(
            [
                new Claim(SessionAuthenticationDefaults.InternalUserIdClaimType, userId.ToString()),
                new Claim(SessionAuthenticationDefaults.SessionIdClaimType, "session-current")
            ],
            csrfRequestValidator: CreateCsrfRequestValidator("https://app.authcore.dev"));

        controller.Request.Headers.Referer = "https://app.authcore.dev/seguranca/sessoes";

        var result = await controller.RevokeSession("session-other", useCase, authCookieOptions);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(userId, useCase.LastCommand!.UserId);
        Assert.Equal("session-other", useCase.LastCommand.SessionId);
    }

    [Fact]
    public async Task RevokeSession_WhenRevokingAnotherSession_ShouldNotDeleteCookie()
    {
        var userId = Guid.NewGuid();
        var useCase = new SpyRevokeUserSessionUseCase();
        var authCookieOptions = Options.Create(new AuthCookieOptions
        {
            SessionCookieName = "sid",
            Secure = false
        });
        var controller = CreateController(new[]
        {
            new Claim(SessionAuthenticationDefaults.InternalUserIdClaimType, userId.ToString()),
            new Claim(SessionAuthenticationDefaults.SessionIdClaimType, "session-current")
        });

        var result = await controller.RevokeSession("session-other", useCase, authCookieOptions);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("session-other", useCase.LastCommand!.SessionId);
        Assert.True(string.IsNullOrWhiteSpace(controller.Response.Headers.SetCookie.ToString()));
    }

    [Fact]
    public async Task RevokeSession_WhenUseCaseThrowsNotFoundException_ShouldReturnNotFoundWithoutDeletingCookie()
    {
        var userId = Guid.NewGuid();
        var useCase = new ThrowingRevokeUserSessionUseCase(new NotFoundException("A sessão informada não foi encontrada para o usuário."));
        var authCookieOptions = Options.Create(new AuthCookieOptions
        {
            SessionCookieName = "sid",
            Secure = false
        });
        var controller = CreateController(new[]
        {
            new Claim(SessionAuthenticationDefaults.InternalUserIdClaimType, userId.ToString()),
            new Claim(SessionAuthenticationDefaults.SessionIdClaimType, "session-current")
        });

        var result = await controller.RevokeSession("session-other", useCase, authCookieOptions);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ResponseErrorJson>(notFoundResult.Value);

        Assert.Equal(["A sessão informada não foi encontrada para o usuário."], response.Errors);
        Assert.True(string.IsNullOrWhiteSpace(controller.Response.Headers.SetCookie.ToString()));
    }

    [Fact]
    public async Task LogoutAll_WhenUseCaseSucceeds_ShouldReturnNoContentAndDeleteCookie()
    {
        var userId = Guid.NewGuid();
        var useCase = new SpyLogoutAllSessionsUseCase();
        var authCookieOptions = Options.Create(new AuthCookieOptions
        {
            SessionCookieName = "sid",
            Secure = false
        });
        var controller = CreateController(new[]
        {
            new Claim(SessionAuthenticationDefaults.InternalUserIdClaimType, userId.ToString()),
            new Claim(SessionAuthenticationDefaults.SessionIdClaimType, "session-123")
        });

        var result = await controller.LogoutAll(useCase, authCookieOptions);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(userId, useCase.LastCommand!.UserId);
        Assert.Contains("sid=", controller.Response.Headers.SetCookie.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task LogoutAll_WhenOriginAndRefererAreMissing_ShouldReturnForbiddenResponseErrorJson()
    {
        var userId = Guid.NewGuid();
        var useCase = new SpyLogoutAllSessionsUseCase();
        var authCookieOptions = Options.Create(new AuthCookieOptions
        {
            SessionCookieName = "sid",
            Secure = false
        });
        var controller = CreateController(
            [
                new Claim(SessionAuthenticationDefaults.InternalUserIdClaimType, userId.ToString()),
                new Claim(SessionAuthenticationDefaults.SessionIdClaimType, "session-123")
            ],
            csrfRequestValidator: CreateCsrfRequestValidator("https://app.authcore.dev"));

        var result = await controller.LogoutAll(useCase, authCookieOptions);

        var forbiddenResult = Assert.IsType<ObjectResult>(result);
        var response = Assert.IsType<ResponseErrorJson>(forbiddenResult.Value);

        Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
        Assert.Equal(["A origem da requisição não é permitida."], response.Errors);
        Assert.Null(useCase.LastCommand);
    }

    [Fact]
    public async Task Login_WhenEmailRateLimitIsExceeded_ShouldReturnTooManyRequests()
    {
        var useCase = new SpyLoginSessionUseCase
        {
            Result = new AuthenticatedUserSessionResult
            {
                SessionId = "session-123",
                UserIdentifier = Guid.NewGuid(),
                Email = "blocked@authcore.dev",
                ExpiresAtUtc = new DateTime(2026, 4, 20, 15, 0, 0, DateTimeKind.Utc)
            }
        };
        var authCookieOptions = Options.Create(new AuthCookieOptions
        {
            SessionCookieName = "sid",
            Secure = false
        });
        var rateLimiter = CreateLoginRateLimiter(new LoginRateLimitOptions
        {
            MaxAttemptsPerIp = 20,
            MaxAttemptsPerEmail = 1,
            WindowMinutes = 5
        });
        var controller = CreateController(loginRateLimiter: rateLimiter);

        controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        _ = await controller.Login(useCase, authCookieOptions, new RequestSessionLoginJson
        {
            Email = "blocked@authcore.dev",
            Password = "ValidPassword#2026"
        });

        var blockedResult = await controller.Login(useCase, authCookieOptions, new RequestSessionLoginJson
        {
            Email = "blocked@authcore.dev",
            Password = "ValidPassword#2026"
        });

        var tooManyRequestsResult = Assert.IsType<ObjectResult>(blockedResult.Result);
        var response = Assert.IsType<ResponseErrorJson>(tooManyRequestsResult.Value);

        Assert.Equal(StatusCodes.Status429TooManyRequests, tooManyRequestsResult.StatusCode);
        Assert.Equal(["Muitas tentativas de login. Aguarde alguns minutos e tente novamente."], response.Errors);
        Assert.Equal("300", controller.Response.Headers["Retry-After"].ToString());
    }

    [Fact]
    public async Task Token_WhenIpRateLimitIsExceeded_ShouldReturnTooManyRequests()
    {
        var useCase = new SpyLoginUseCase
        {
            Result = new AuthenticatedSessionResult
            {
                AccessToken = "access-token",
                AccessTokenExpiresAtUtc = new DateTime(2026, 4, 13, 15, 0, 0, DateTimeKind.Utc),
                RefreshToken = "refresh-token",
                RefreshTokenExpiresAtUtc = new DateTime(2026, 4, 20, 15, 0, 0, DateTimeKind.Utc)
            }
        };
        var rateLimiter = CreateLoginRateLimiter(new LoginRateLimitOptions
        {
            MaxAttemptsPerIp = 1,
            MaxAttemptsPerEmail = 20,
            WindowMinutes = 5
        });
        var controller = CreateController(loginRateLimiter: rateLimiter);

        controller.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        _ = await controller.Token(useCase, new RequestLoginJson
        {
            Email = "first@authcore.dev",
            Password = "ValidPassword#2026"
        });

        var blockedResult = await controller.Token(useCase, new RequestLoginJson
        {
            Email = "second@authcore.dev",
            Password = "ValidPassword#2026"
        });

        var tooManyRequestsResult = Assert.IsType<ObjectResult>(blockedResult.Result);
        var response = Assert.IsType<ResponseErrorJson>(tooManyRequestsResult.Value);

        Assert.Equal(StatusCodes.Status429TooManyRequests, tooManyRequestsResult.StatusCode);
        Assert.Equal(["Muitas tentativas de login. Aguarde alguns minutos e tente novamente."], response.Errors);
    }

    [Fact]
    public void Build_WhenReverseProxyIsConfigured_ShouldBindForwardedHeadersOptions()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Jwt:Issuer"] = "authcore-tests",
            ["Authentication:Jwt:Audience"] = "authcore-tests",
            ["Authentication:Jwt:SigningKey"] = "12345678901234567890123456789012",
            ["Authentication:Jwt:AccessTokenLifetimeMinutes"] = "15",
            ["Authentication:Jwt:RefreshTokenLifetimeDays"] = "7",
            ["Authentication:Jwt:ClockSkewSeconds"] = "60",
            ["Auth:Cookie:SessionCookieName"] = "sid",
            ["Auth:Cookie:Secure"] = "false",
            ["ReverseProxy:KnownProxies:0"] = "10.0.0.10",
            ["ReverseProxy:KnownNetworks:0"] = "10.10.0.0/24",
            ["ReverseProxy:ForwardLimit"] = "3"
        });

        builder.Services.AddApi(builder.Configuration);

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var forwardedHeadersOptions = serviceProvider
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>()
            .Value;

        Assert.Equal(3, forwardedHeadersOptions.ForwardLimit);
        Assert.Contains(forwardedHeadersOptions.KnownProxies, proxy => proxy.ToString() == "10.0.0.10");
        Assert.Contains(
            forwardedHeadersOptions.KnownNetworks,
            network => network.Prefix.ToString() == "10.10.0.0" && network.PrefixLength == 24);
    }

    [Fact]
    public async Task Build_WhenMvcIsConfigured_ShouldDiscoverAuthEndpoints()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Jwt:Issuer"] = "authcore-tests",
            ["Authentication:Jwt:Audience"] = "authcore-tests",
            ["Authentication:Jwt:SigningKey"] = "12345678901234567890123456789012",
            ["Authentication:Jwt:AccessTokenLifetimeMinutes"] = "15",
            ["Authentication:Jwt:RefreshTokenLifetimeDays"] = "7",
            ["Authentication:Jwt:ClockSkewSeconds"] = "60",
            ["Auth:Cookie:SessionCookieName"] = "sid",
            ["Auth:Cookie:Secure"] = "false"
        });

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(AuthController).Assembly);

        builder.Services.AddApi(builder.Configuration);

        await using var app = builder.Build();
        await using var scope = app.Services.CreateAsyncScope();
        var actionProvider = scope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>();
        var actions = actionProvider.ActionDescriptors.Items
            .Where(action => string.Equals(action.RouteValues["controller"], "Auth", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Contains(actions, action => action.AttributeRouteInfo?.Template == "api/auth/login");
        Assert.Contains(actions, action => action.AttributeRouteInfo?.Template == "api/auth/token");
        Assert.Contains(actions, action => action.AttributeRouteInfo?.Template == "api/auth/refresh");
        Assert.Contains(actions, action => action.AttributeRouteInfo?.Template == "api/auth/token/logout");
        Assert.Contains(actions, action => action.AttributeRouteInfo?.Template == "api/auth/me");
        Assert.Contains(actions, action => action.AttributeRouteInfo?.Template == "api/auth/logout");
        Assert.Contains(actions, action => action.AttributeRouteInfo?.Template == "api/auth/sessions");
        Assert.Contains(actions, action => action.AttributeRouteInfo?.Template == "api/auth/sessions/{sid}");
        Assert.Contains(actions, action => action.AttributeRouteInfo?.Template == "api/auth/logout-all");
    }

    #region Helpers

    private static AuthController CreateController(
        IEnumerable<Claim>? claims = null,
        ICsrfRequestValidator? csrfRequestValidator = null,
        ILoginRateLimiter? loginRateLimiter = null)
    {
        var httpContext = new DefaultHttpContext();

        if (claims is not null)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                claims,
                SessionAuthenticationDefaults.AuthenticationScheme));
        }

        return new AuthController(
            csrfRequestValidator ?? new AllowAllCsrfRequestValidator(),
            loginRateLimiter ?? new AllowAllLoginRateLimiter(),
            NullLogger<AuthController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    private static ICsrfRequestValidator CreateCsrfRequestValidator(params string[] allowedOrigins)
    {
        return new CookieCsrfRequestValidator(
            NullLogger<CookieCsrfRequestValidator>.Instance,
            Options.Create(new CsrfOptions
            {
                AllowedOrigins = allowedOrigins
            }));
    }

    private static ILoginRateLimiter CreateLoginRateLimiter(LoginRateLimitOptions options)
    {
        return new InMemoryLoginRateLimiter(
            new FakeTimeProvider(new DateTimeOffset(2026, 4, 19, 12, 0, 0, TimeSpan.Zero)),
            Options.Create(options));
    }

    private sealed class SpyLoginSessionUseCase : ILoginSessionUseCase
    {
        public LoginSessionCommand? LastCommand { get; private set; }

        public AuthenticatedUserSessionResult Result { get; set; } = new();

        public Task<AuthenticatedUserSessionResult> Execute(LoginSessionCommand command)
        {
            LastCommand = command;
            return Task.FromResult(Result);
        }
    }

    private sealed class ThrowingLoginSessionUseCase : ILoginSessionUseCase
    {
        private readonly Exception _exception;

        public ThrowingLoginSessionUseCase(Exception exception)
        {
            _exception = exception;
        }

        public Task<AuthenticatedUserSessionResult> Execute(LoginSessionCommand command)
        {
            return Task.FromException<AuthenticatedUserSessionResult>(_exception);
        }
    }

    private sealed class SpyLoginUseCase : ILoginUseCase
    {
        public LoginCommand? LastCommand { get; private set; }

        public AuthenticatedSessionResult Result { get; set; } = new();

        public Task<AuthenticatedSessionResult> Execute(LoginCommand command)
        {
            LastCommand = command;
            return Task.FromResult(Result);
        }
    }

    private sealed class SpyLogoutCurrentSessionUseCase : ILogoutCurrentSessionUseCase
    {
        public LogoutCurrentSessionCommand? LastCommand { get; private set; }

        public Task Execute(LogoutCurrentSessionCommand command)
        {
            LastCommand = command;
            return Task.CompletedTask;
        }
    }

    private sealed class SpyLogoutSessionUseCase : ILogoutSessionUseCase
    {
        public LogoutSessionCommand? LastCommand { get; private set; }

        public Task Execute(LogoutSessionCommand command)
        {
            LastCommand = command;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingLogoutSessionUseCase : ILogoutSessionUseCase
    {
        private readonly Exception _exception;

        public ThrowingLogoutSessionUseCase(Exception exception)
        {
            _exception = exception;
        }

        public Task Execute(LogoutSessionCommand command)
        {
            return Task.FromException(_exception);
        }
    }

    private sealed class SpyGetUserSessionsUseCase : IGetUserSessionsUseCase
    {
        public GetUserSessionsQuery? LastQuery { get; private set; }

        public UserSessionsResult Result { get; set; } = new();

        public Task<UserSessionsResult> Execute(GetUserSessionsQuery query)
        {
            LastQuery = query;
            return Task.FromResult(Result);
        }
    }

    private sealed class SpyRefreshSessionUseCase : IRefreshSessionUseCase
    {
        public RefreshSessionCommand? LastCommand { get; private set; }

        public AuthenticatedSessionResult Result { get; set; } = new();

        public Task<AuthenticatedSessionResult> Execute(RefreshSessionCommand command)
        {
            LastCommand = command;
            return Task.FromResult(Result);
        }
    }

    private sealed class SpyRevokeUserSessionUseCase : IRevokeUserSessionUseCase
    {
        public RevokeUserSessionCommand? LastCommand { get; private set; }

        public Task Execute(RevokeUserSessionCommand command)
        {
            LastCommand = command;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingRevokeUserSessionUseCase : IRevokeUserSessionUseCase
    {
        private readonly Exception _exception;

        public ThrowingRevokeUserSessionUseCase(Exception exception)
        {
            _exception = exception;
        }

        public Task Execute(RevokeUserSessionCommand command)
        {
            return Task.FromException(_exception);
        }
    }

    private sealed class SpyLogoutAllSessionsUseCase : ILogoutAllSessionsUseCase
    {
        public LogoutAllSessionsCommand? LastCommand { get; private set; }

        public Task Execute(LogoutAllSessionsCommand command)
        {
            LastCommand = command;
            return Task.CompletedTask;
        }
    }

    private sealed class AllowAllCsrfRequestValidator : ICsrfRequestValidator
    {
        public void Validate(HttpRequest request)
        {
        }
    }

    private sealed class AllowAllLoginRateLimiter : ILoginRateLimiter
    {
        public Task<LoginRateLimitResult> TryAcquireAsync(string? ipAddress, string? email)
        {
            return Task.FromResult(LoginRateLimitResult.Allow());
        }
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FakeTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }

    #endregion
}
