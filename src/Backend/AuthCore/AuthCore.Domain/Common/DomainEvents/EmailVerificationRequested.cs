using AuthCore.Domain.Common.Exceptions;

namespace AuthCore.Domain.Common.DomainEvents;

/// <summary>
/// Representa o evento de solicitação de verificação de e-mail.
/// </summary>
public sealed class EmailVerificationRequested
{
    /// <summary>
    /// Identificador interno do usuário.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// E-mail de destino.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Código OTP em texto puro.
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Data da solicitação em UTC.
    /// </summary>
    public DateTime RequestedAtUtc { get; init; }

    /// <summary>
    /// Operação para validar a consistência do evento.
    /// </summary>
    public void Validate()
    {
        DomainException.When(UserId == Guid.Empty, "O identificador do usuário do evento é obrigatório.");
        DomainException.When(string.IsNullOrWhiteSpace(Email), "O e-mail do evento é obrigatório.");
        DomainException.When(string.IsNullOrWhiteSpace(Code), "O código do evento é obrigatório.");
        DomainException.When(RequestedAtUtc == default, "A data de solicitação do evento é obrigatória.");
    }
}
