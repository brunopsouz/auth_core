using AuthCore.Domain.Common.Exceptions;
using AuthCore.Domain.Common.Repositories;
using AuthCore.Domain.Passports.Aggregates;
using AuthCore.Domain.Passports.Repositories;
using AuthCore.Domain.Security.Cryptography;
using AuthCore.Domain.Common.Enums;
using AuthCore.Domain.Users.Aggregates;
using AuthCore.Domain.Users.Enums;
using AuthCore.Domain.Users.Repositories;

namespace AuthCore.Application.Users.UseCases.RegisterUser;

/// <summary>
/// Representa caso de uso para registrar um usuário.
/// </summary>
public sealed class RegisterUserUseCase : IRegisterUserUseCase
{
    private readonly IPasswordEncripter _passwordEncripter;
    private readonly IPasswordRepository _passwordRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserReadRepository _userReadRepository;
    private readonly IUserRepository _userRepository;

    #region Constructors

    /// <summary>
    /// Operação para criar instância da classe.
    /// </summary>
    /// <param name="userRepository">Repositório de escrita de usuário.</param>
    /// <param name="userReadRepository">Repositório de leitura de usuário.</param>
    /// <param name="passwordRepository">Repositório de senha.</param>
    /// <param name="passwordEncripter">Serviço de criptografia de senha.</param>
    /// <param name="unitOfWork">Unidade de trabalho transacional.</param>
    public RegisterUserUseCase(
        IUserRepository userRepository,
        IUserReadRepository userReadRepository,
        IPasswordRepository passwordRepository,
        IPasswordEncripter passwordEncripter,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _userReadRepository = userReadRepository;
        _passwordRepository = passwordRepository;
        _passwordEncripter = passwordEncripter;
        _unitOfWork = unitOfWork;
    }

    #endregion

    /// <summary>
    /// Operação para registrar um usuário.
    /// </summary>
    /// <param name="command">Comando com os dados do registro.</param>
    /// <returns>Resultado do usuário registrado.</returns>
    public async Task<RegisterUserResult> Execute(RegisterUserCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        Password.ValidateWithConfirmation(command.Password, command.ConfirmPassword);

        var existingUser = await _userReadRepository.GetByEmailAsync(command.Email);

        if (existingUser is not null)
            throw new DomainException("Já existe um usuário cadastrado com o e-mail informado.");

        var user = User.Register(
            command.FirstName,
            command.LastName,
            command.Email,
            command.Contact,
            Role.User);

        var passwordHash = _passwordEncripter.Encrypt(command.Password);
        var password = Password.Create(user.Id, passwordHash, PasswordStatus.FirstAccess);

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            await _userRepository.AddAsync(user);
            await _passwordRepository.AddAsync(password);
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }

        return new RegisterUserResult
        {
            UserIdentifier = user.UserIdentifier,
            FullName = user.FullName,
            Email = user.Email.Value
        };
    }
}
