namespace AuthCore.Api.Contracts.Requests;

/// <summary>
/// Representa a requisição de reenvio da verificação de e-mail.
/// </summary>
public sealed class RequestResendVerificationJson
{
    /// <summary>
    /// E-mail do usuário.
    /// </summary>
    public string Email { get; init; } = string.Empty;
}
