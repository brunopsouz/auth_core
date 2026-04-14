namespace AuthCore.Api.Contracts.Requests;

/// <summary>
/// Representa requisição para autenticar um usuário.
/// </summary>
public sealed class RequestLoginJson
{
    /// <summary>
    /// E-mail do usuário.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Senha do usuário.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
