using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AuthCore.Domain.Common.Enums;
using AuthCore.Domain.Common.Exceptions;

namespace AuthCore.Domain.Entities;

/// <summary>
/// Entidade responsável pelo gerenciamento da senha do usuário.
/// </summary>
public class Password
{
    private const int MIN_LENGTH = 8;
    private const int MAX_LENGTH = 24;
    
    /// <summary>
    /// Identificador único do usuário associado à senha.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Valor da senha armazenada para o usuário.
    /// </summary>
    public string Value { get; private set; } = null!;

    // /// <summary>
    // /// Chave secreta utilizada para proteger ou validar a senha.
    // /// </summary>
    // public string? SecretKey { get; private set; } = null!;

    /// <summary>
    /// Número de tentativas de acesso realizadas pelo usuário.
    /// </summary>
    public LoginAttempt LoginAttempt { get; private set; } = null!;

    /// <summary>
    /// Status atual da senha (ex.: ativa, bloqueada, expirada).
    /// </summary>
    public PasswordStatus Status { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="value"></param>
    /// <param name="secretKey"></param>
    /// <param name="attempts"></param>
    /// <param name="status"></param>
    protected Password(
        Guid userId,
        string hashedPassword,
        LoginAttempt attempts,
        PasswordStatus status)
    {
        UserId = userId;
        Value = hashedPassword;
        Status = status;
        LoginAttempt= attempts;
    }

    private Password() { }

    /// <summary>
    /// Cria a senha com o valor já criptografado.
    /// </summary>
    /// <param name="hashedPassword">Senha criptografada.</param>
    /// <param name="status">Status atual da senha (ex.: ativa, bloqueada, expirada).</param>
    /// <returns>Instância criada de <see cref="Password"/>.</returns>
    public static Password Create(
        string hashedPassword,
        PasswordStatus status)
    {
        return new Password(
            Guid.NewGuid(),
            hashedPassword,
            attempts: LoginAttempt.Create(),
            status
        );
    }

    /// <summary>
    /// Valida os critérios mínimos da senha.
    /// </summary>
    /// <param name="password">Senha a validar.</param>
    /// <exception cref="BadRequestException">Lançada quando o formato é inválido.</exception>
    public static void Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new DomainException("A senha não pode estar vazia.");

        if (password.Length < MIN_LENGTH || password.Length > MAX_LENGTH)
            throw new DomainException($"A senha deve conter entre {MIN_LENGTH} e {MAX_LENGTH} caracteres.");

        var regex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,24}$");

        if (!regex.IsMatch(password))
            throw new DomainException("A senha deve conter letras maiúsculas, minúsculas, números e caracteres especiais.");
    }

    /// <summary>
    /// Valida a senha e confirma se ambas correspondem.
    /// </summary>
    /// <param name="password">Senha informada.</param>
    /// <param name="confirmPassword">Confirmação da senha.</param>
    /// <exception cref="BadRequestException">Lançada quando os valores não são compatíveis.</exception>
    public static void ValidateWithConfirmation(string password, string confirmPassword)
    {
        Validate(password);

        if (string.IsNullOrWhiteSpace(confirmPassword))
            throw new DomainException("A confirmação de senha não pode estar vazia.");

        if (password != confirmPassword)
            throw new DomainException("As senhas não correspondem.");
    }

    private void ValidateLoginAttempts()
    {
        if (LoginAttempt is null)
            throw new Exception("Os dados de tentativas de login são obrigatórios.");
    }

}