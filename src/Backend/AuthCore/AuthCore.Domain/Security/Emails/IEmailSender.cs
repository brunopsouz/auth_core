namespace AuthCore.Domain.Security.Emails;

/// <summary>
/// Define operação para envio de e-mail de verificação.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Operação para enviar o código de verificação de e-mail.
    /// </summary>
    /// <param name="email">E-mail de destino.</param>
    /// <param name="code">Código OTP em texto puro.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    Task SendEmailVerificationAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default);
}
