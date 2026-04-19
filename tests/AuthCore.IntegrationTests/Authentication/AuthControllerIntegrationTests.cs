using System.Security.Claims;
using AuthCore.Api;
using AuthCore.Api.Authentication;
using AuthCore.Api.Contracts.Requests;
using AuthCore.Api.Contracts.Responses;
using AuthCore.Api.Controllers;
using AuthCore.Application.Authentication.Models;
using AuthCore.Application.Authentication.UseCases.Login;
using AuthCore.Application.Authentication.UseCases.LoginSession;
using AuthCore.Application.Authentication.UseCases.LogoutCurrentSession;
using AuthCore.Application.Authentication.UseCases.RefreshSession;
using AuthCore.Application.Common.Models.Responses;
using AuthCore.Domain.Common.Exceptions;
using AuthCore.Infrastructure.Configurations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        Assert.Contains(actions, action => action.AttributeRouteInfo?.Template == "api/auth/me");
        Assert.Contains(actions, action => action.AttributeRouteInfo?.Template == "api/auth/logout");
    }

    #region Helpers

    private static AuthController CreateController(IEnumerable<Claim>? claims = null)
    {
        var httpContext = new DefaultHttpContext();

        if (claims is not null)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                claims,
                SessionAuthenticationDefaults.AuthenticationScheme));
        }

        return new AuthController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
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

    #endregion
}
