using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace AuthCore.Api.Contracts;

/// <summary>
/// Representa atributo para exigir usuário autenticado.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AuthenticatedUserAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Operação para criar instância da classe.
    /// </summary>
    public AuthenticatedUserAttribute()
    {
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme;
    }
}
