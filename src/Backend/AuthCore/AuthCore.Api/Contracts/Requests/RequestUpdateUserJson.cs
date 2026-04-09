namespace AuthCore.Api.Contracts.Requests;

/// <summary>
/// Representa requisição para atualizar um usuário.
/// </summary>
public sealed class RequestUpdateUserJson
{
    /// <summary>
    /// Primeiro nome do usuário.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Sobrenome do usuário.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Número de contato do usuário.
    /// </summary>
    public string Contact { get; set; } = string.Empty;
}
