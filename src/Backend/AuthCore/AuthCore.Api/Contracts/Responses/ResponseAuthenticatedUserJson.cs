namespace AuthCore.Api.Contracts.Responses;

/// <summary>
/// Representa resposta do usuário autenticado por sessão.
/// </summary>
public sealed class ResponseAuthenticatedUserJson
{
    /// <summary>
    /// Identificador público do usuário autenticado.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// E-mail do usuário autenticado.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
