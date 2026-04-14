using AuthCore.Api;
using AuthCore.Api.Contracts.Requests;
using AuthCore.Api.Contracts.Responses;
using AuthCore.Api.Controllers;
using AuthCore.Application.Authentication.Models;
using AuthCore.Application.Authentication.UseCases.Login;
using AuthCore.Application.Authentication.UseCases.LogoutSession;
using AuthCore.Application.Authentication.UseCases.RefreshSession;
using AuthCore.Application.Common.Models.Responses;
using AuthCore.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthCore.IntegrationTests.Authentication;

/// <summary>
/// Verifica a exposição e o comportamento HTTP do controller de autenticação.
/// </summary>
public sealed class AuthControllerIntegrationTests
{
    [Fact]
    public async Task Login_WhenUseCaseSucceeds_ShouldReturnOkWithAuthenticatedSessionResponse()
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
        var controller = new AuthController();

        var result = await controller.Login(useCase, new RequestLoginJson
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
    public async Task Refresh_WhenUseCaseThrowsUnauthorizedAccessException_ShouldReturnUnauthorizedResponseErrorJson()
    {
        var useCase = new ThrowingRefreshSessionUseCase(new UnauthorizedAccessException("A sessão informada é inválida ou expirou."));
        var controller = new AuthController();

        var result = await controller.Refresh(useCase, new RequestRefreshSessionJson
        {
            RefreshToken = "expired-refresh-token"
        });

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var response = Assert.IsType<ResponseErrorJson>(unauthorizedResult.Value);

        Assert.Equal(["A sessão informada é inválida ou expirou."], response.Errors);
    }

    [Fact]
    public async Task Logout_WhenUseCaseSucceeds_ShouldReturnNoContent()
    {
        var useCase = new SpyLogoutSessionUseCase();
        var controller = new AuthController();

        var result = await controller.Logout(useCase, new RequestLogoutSessionJson
        {
            RefreshToken = "refresh-token"
        });

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("refresh-token", useCase.LastCommand!.RefreshToken);
    }

    [Fact]
    public async Task Logout_WhenUseCaseThrowsArgumentException_ShouldReturnBadRequestResponseErrorJson()
    {
        var useCase = new ThrowingLogoutSessionUseCase(new ArgumentException("O refresh token é obrigatório.", "refreshToken"));
        var controller = new AuthController();

        var result = await controller.Logout(useCase, new RequestLogoutSessionJson());

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ResponseErrorJson>(badRequestResult.Value);

        Assert.Equal(["O refresh token é obrigatório. (Parameter 'refreshToken')"], response.Errors);
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
            ["Authentication:Jwt:ClockSkewSeconds"] = "60"
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
        Assert.Contains(actions, action => action.AttributeRouteInfo?.Template == "api/auth/refresh");
        Assert.Contains(actions, action => action.AttributeRouteInfo?.Template == "api/auth/logout");
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

    private sealed class ThrowingRefreshSessionUseCase : IRefreshSessionUseCase
    {
        private readonly Exception _exception;

        public ThrowingRefreshSessionUseCase(Exception exception)
        {
            _exception = exception;
        }

        public Task<AuthenticatedSessionResult> Execute(RefreshSessionCommand command)
        {
            return Task.FromException<AuthenticatedSessionResult>(_exception);
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
}
