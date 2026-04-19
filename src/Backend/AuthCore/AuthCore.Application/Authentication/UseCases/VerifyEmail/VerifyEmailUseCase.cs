using AuthCore.Domain.Common.Exceptions;
using AuthCore.Domain.Common.Repositories;
using AuthCore.Domain.Passports.Repositories;
using AuthCore.Domain.Passports.Services;
using AuthCore.Domain.Users.Repositories;

namespace AuthCore.Application.Authentication.UseCases.VerifyEmail;

/// <summary>
/// Representa caso de uso para verificar o e-mail do usuário.
/// </summary>
public sealed class VerifyEmailUseCase : IVerifyEmailUseCase
{
    private readonly IEmailVerificationRepository _emailVerificationRepository;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserReadRepository _userReadRepository;
    private readonly IUserRepository _userRepository;

    #region Constructors

    /// <summary>
    /// Operação para criar instância da classe.
    /// </summary>
    /// <param name="emailVerificationRepository">Repositório de verificação de e-mail.</param>
    /// <param name="emailVerificationService">Serviço de verificação de e-mail.</param>
    /// <param name="userReadRepository">Repositório de leitura de usuário.</param>
    /// <param name="userRepository">Repositório de escrita de usuário.</param>
    /// <param name="unitOfWork">Unidade de trabalho transacional.</param>
    public VerifyEmailUseCase(
        IEmailVerificationRepository emailVerificationRepository,
        IEmailVerificationService emailVerificationService,
        IUserReadRepository userReadRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _emailVerificationRepository = emailVerificationRepository;
        _emailVerificationService = emailVerificationService;
        _userReadRepository = userReadRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    #endregion

    /// <summary>
    /// Operação para verificar o e-mail do usuário.
    /// </summary>
    /// <param name="command">Comando com o e-mail e código OTP.</param>
    public async Task Execute(VerifyEmailCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalizedEmail = NormalizeEmail(command.Email);
        var emailVerification = await _emailVerificationRepository.GetPendingByEmailAsync(normalizedEmail)
            ?? throw new NotFoundException("Nenhuma verificação pendente foi encontrada para o e-mail informado.");
        var user = await _userReadRepository.GetByEmailAsync(normalizedEmail)
            ?? throw new NotFoundException("Usuário não encontrado.");
        var validatedVerification = emailVerification.ValidateCode(
            _emailVerificationService.ComputeHash(command.Code),
            DateTime.UtcNow);

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            await _emailVerificationRepository.UpdateAsync(validatedVerification);

            if (!validatedVerification.ConsumedAtUtc.HasValue)
                throw new DomainException("O código de verificação informado é inválido.");

            user.VerifyEmail(validatedVerification.ConsumedAtUtc.Value);
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    #region Helpers

    /// <summary>
    /// Operação para normalizar o e-mail informado.
    /// </summary>
    /// <param name="email">E-mail informado.</param>
    /// <returns>E-mail normalizado.</returns>
    private static string NormalizeEmail(string email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? string.Empty
            : email.Trim().ToLowerInvariant();
    }

    #endregion
}
