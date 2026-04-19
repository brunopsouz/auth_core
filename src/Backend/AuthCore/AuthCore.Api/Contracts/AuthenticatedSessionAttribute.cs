using AuthCore.Api.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace AuthCore.Api.Contracts;

/// <summary>
/// Representa atributo para exigir sessão autenticada por cookie.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AuthenticatedSessionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Operação para criar instância da classe.
    /// </summary>
    public AuthenticatedSessionAttribute()
    {
        AuthenticationSchemes = SessionAuthenticationDefaults.AuthenticationScheme;
    }
}
