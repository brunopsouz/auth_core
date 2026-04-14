using AuthCore.Application.Authentication.UseCases.Login;
using AuthCore.Application.Authentication.UseCases.LogoutSession;
using AuthCore.Application.Authentication.UseCases.RefreshSession;
using AuthCore.Application.Users.UseCases.ChangePassword;
using AuthCore.Application.Users.UseCases.DeleteUser;
using AuthCore.Application.Users.UseCases.GetUserProfile;
using AuthCore.Application.Users.UseCases.RegisterUser;
using AuthCore.Application.Users.UseCases.UpdateUser;
using Microsoft.Extensions.DependencyInjection;

namespace AuthCore.Application;

/// <summary>
/// Define operações para registrar dependências da aplicação.
/// </summary>
public static class ApplicationDependencyInjection
{
    /// <summary>
    /// Operação para adicionar os serviços da aplicação.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <returns>Coleção de serviços atualizada.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ILoginUseCase, LoginUseCase>();
        services.AddScoped<ILogoutSessionUseCase, LogoutSessionUseCase>();
        services.AddScoped<IRefreshSessionUseCase, RefreshSessionUseCase>();
        services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
        services.AddScoped<IGetUserProfileUseCase, GetUserProfileUseCase>();
        services.AddScoped<IUpdateUserUseCase, UpdateUserUseCase>();
        services.AddScoped<IChangePasswordUseCase, ChangePasswordUseCase>();
        services.AddScoped<IDeleteUserUseCase, DeleteUserUseCase>();

        return services;
    }
}
