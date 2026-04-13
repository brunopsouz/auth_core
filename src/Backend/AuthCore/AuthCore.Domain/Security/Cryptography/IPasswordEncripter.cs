namespace AuthCore.Domain.Security.Cryptography;

/// <summary>
/// Define operações para criptografar e validar senha.
/// </summary>
public interface IPasswordEncripter
{
    /// <summary>
    /// Operação para criptografar uma senha.
    /// </summary>
    /// <param name="password">Senha em texto puro.</param>
    /// <returns>Senha criptografada.</returns>
    string Encrypt(string password);

    /// <summary>
    /// Operação para validar uma senha com o valor criptografado.
    /// </summary>
    /// <param name="password">Senha em texto puro.</param>
    /// <param name="passwordHash">Senha criptografada para validação.</param>
    /// <returns><c>true</c> quando a senha é válida; caso contrário, <c>false</c>.</returns>
    bool IsValid(string password, string passwordHash);
}
