using AuthCore.Application.Authentication.Models;
using AuthCore.Domain.Common.Enums;
using AuthCore.Domain.Common.Repositories;
using AuthCore.Domain.Passports.Aggregates;
using AuthCore.Domain.Passports.Repositories;
using AuthCore.Domain.Security.Cryptography;
using AuthCore.Domain.Security.Tokens.Services;
using AuthCore.Domain.Users.Aggregates;
using AuthCore.Domain.Users.Repositories;

namespace AuthCore.Application.Authentication.UseCases.Login;

/// <summary>
/// Representa caso de uso para autenticar um usuário.
/// </summary>
public sealed class LoginUseCase : ILoginUseCase
{
    private const string INVALID_CREDENTIALS_MESSAGE = "As credenciais informadas são inválidas.";

    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly IPasswordEncripter _passwordEncripter;
    private readonly IPasswordRepository _passwordRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserReadRepository _userReadRepository;

    #region Constructors

    /// <summary>
    /// Operação para criar instância da classe.
    /// </summary>
    /// <param name="userReadRepository">Repositório de leitura de usuário.</param>
    /// <param name="passwordRepository">Repositório de senha.</param>
    /// <param name="passwordEncripter">Serviço de criptografia de senha.</param>
    /// <param name="accessTokenGenerator">Gerador de access token.</param>
    /// <param name="refreshTokenService">Serviço de refresh token.</param>
    /// <param name="refreshTokenRepository">Repositório de refresh token.</param>
    /// <param name="unitOfWork">Unidade de trabalho transacional.</param>
    public LoginUseCase(
        IUserReadRepository userReadRepository,
        IPasswordRepository passwordRepository,
        IPasswordEncripter passwordEncripter,
        IAccessTokenGenerator accessTokenGenerator,
        IRefreshTokenService refreshTokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork)
    {
        _userReadRepository = userReadRepository;
        _passwordRepository = passwordRepository;
        _passwordEncripter = passwordEncripter;
        _accessTokenGenerator = accessTokenGenerator;
        _refreshTokenService = refreshTokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
    }

    #endregion

    /// <summary>
    /// Operação para autenticar um usuário.
    /// </summary>
    /// <param name="command">Comando com as credenciais do login.</param>
    /// <returns>Resultado da sessão autenticada.</returns>
    public async Task<AuthenticatedSessionResult> Execute(LoginCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var email = NormalizeEmail(command.Email);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(command.Password))
            throw CreateInvalidCredentialsException();

        var user = await _userReadRepository.GetByEmailAsync(email);

        if (user is null)
            throw CreateInvalidCredentialsException();

        var password = await _passwordRepository.GetByUserIdAsync(user.Id);

        if (password is null)
            throw CreateInvalidCredentialsException();

        if (!user.CanSignIn || !CanAuthenticate(password))
            throw CreateInvalidCredentialsException();

        if (!_passwordEncripter.IsValid(command.Password, password.Value))
        {
            await RegisterLoginFailureAsync(password);
            throw CreateInvalidCredentialsException();
        }

        return await AuthenticateAsync(user, password);
    }

    #region Helpers

    /// <summary>
    /// Operação para concluir a autenticação com emissão inicial da sessão.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="password">Senha válida do usuário.</param>
    /// <returns>Resultado da sessão autenticada.</returns>
    private async Task<AuthenticatedSessionResult> AuthenticateAsync(User user, Password password)
    {
        var accessToken = _accessTokenGenerator.Generate(user);
        var refreshTokenMaterial = _refreshTokenService.Create();
        var refreshTokenExpiresAtUtc = _refreshTokenService.GetExpiresAtUtc();
        var refreshToken = RefreshToken.IssueInitial(user.Id, refreshTokenMaterial.Hash, refreshTokenExpiresAtUtc);
        var updatedPassword = ShouldResetLoginAttempts(password)
            ? password.ResetLoginAttempts(GetAuthenticatedPasswordStatus(password))
            : null;

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            if (updatedPassword is not null)
                await _passwordRepository.UpdateAsync(updatedPassword);

            await _refreshTokenRepository.AddAsync(refreshToken);
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }

        return new AuthenticatedSessionResult
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshToken = refreshTokenMaterial.Token,
            RefreshTokenExpiresAtUtc = refreshToken.ExpiresAtUtc
        };
    }

    /// <summary>
    /// Operação para registrar uma falha de autenticação na senha persistida.
    /// </summary>
    /// <param name="password">Senha a ser atualizada.</param>
    private async Task RegisterLoginFailureAsync(Password password)
    {
        var updatedPassword = password.RegisterLoginFailure();

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            await _passwordRepository.UpdateAsync(updatedPassword);
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Operação para verificar se a senha pode ser usada na autenticação.
    /// </summary>
    /// <param name="password">Senha do usuário.</param>
    /// <returns><c>true</c> quando a senha pode autenticar; caso contrário, <c>false</c>.</returns>
    private static bool CanAuthenticate(Password password)
    {
        return password.Status is PasswordStatus.Active or PasswordStatus.FirstAccess
            && !password.IsLocked();
    }

    /// <summary>
    /// Operação para indicar se o login bem-sucedido precisa limpar o histórico de falhas.
    /// </summary>
    /// <param name="password">Senha autenticada.</param>
    /// <returns><c>true</c> quando as tentativas devem ser resetadas; caso contrário, <c>false</c>.</returns>
    private static bool ShouldResetLoginAttempts(Password password)
    {
        return password.LoginAttempt.FailedAttempts > 0 || password.IsLocked();
    }

    /// <summary>
    /// Operação para obter o status que a senha deve manter após autenticação válida.
    /// </summary>
    /// <param name="password">Senha autenticada.</param>
    /// <returns>Status consistente após reset das tentativas.</returns>
    private static PasswordStatus GetAuthenticatedPasswordStatus(Password password)
    {
        return password.Status == PasswordStatus.FirstAccess
            ? PasswordStatus.FirstAccess
            : PasswordStatus.Active;
    }

    /// <summary>
    /// Operação para normalizar o e-mail informado no login.
    /// </summary>
    /// <param name="email">E-mail informado.</param>
    /// <returns>E-mail normalizado.</returns>
    private static string NormalizeEmail(string email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? string.Empty
            : email.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Operação para criar a falha genérica de autenticação.
    /// </summary>
    /// <returns>Exceção de acesso não autorizado.</returns>
    private static UnauthorizedAccessException CreateInvalidCredentialsException()
    {
        return new UnauthorizedAccessException(INVALID_CREDENTIALS_MESSAGE);
    }

    #endregion
}
