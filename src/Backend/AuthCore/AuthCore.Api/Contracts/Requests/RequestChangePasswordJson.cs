namespace AuthCore.Api.Contracts.Requests;

/// <summary>
/// Representa requisição para alterar a senha do usuário.
/// </summary>
public sealed class RequestChangePasswordJson
{
    /// <summary>
    /// Senha atual do usuário.
    /// </summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// Nova senha do usuário.
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmação da nova senha do usuário.
    /// </summary>
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
