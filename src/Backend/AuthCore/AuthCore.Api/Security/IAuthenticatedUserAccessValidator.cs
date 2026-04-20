using System.Security.Claims;

namespace AuthCore.Api.Security;

/// <summary>
/// Define operação para revalidar o usuário autenticado no modo bearer.
/// </summary>
public interface IAuthenticatedUserAccessValidator
{
    /// <summary>
    /// Operação para validar o acesso do usuário autenticado e retornar seu identificador público atual.
    /// </summary>
    /// <param name="user">Principal autenticado da requisição atual.</param>
    /// <returns>Identificador público do usuário revalidado.</returns>
    Task<Guid> ValidateAndGetUserIdentifierAsync(ClaimsPrincipal user);
}
