namespace AuthCore.Application.Users.Models.Responses;

/// <summary>
/// Representa resposta de usuário registrado.
/// </summary>
public sealed class ResponseRegisteredUserJson
{
    /// <summary>
    /// Identificador público do usuário.
    /// </summary>
    public Guid UserIdentifier { get; set; }

    /// <summary>
    /// Nome completo do usuário.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// E-mail do usuário.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
