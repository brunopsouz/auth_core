namespace AuthCore.Infrastructure.Persistences.Migrations.Versions;

/// <summary>
/// Define as versões ordenadas das migrações do banco.
/// </summary>
public static class DatabaseVersions
{
    /// <summary>
    /// Versão de criação da tabela de usuários.
    /// </summary>
    public const long TABLE_USERS = 1;

    /// <summary>
    /// Versão de criação da tabela de senhas.
    /// </summary>
    public const long TABLE_PASSWORDS = 2;

    /// <summary>
    /// Versão de inclusão da verificação de e-mail em usuários.
    /// </summary>
    public const long USERS_EMAIL_VERIFICATION = 3;

    /// <summary>
    /// Versão de criação da tabela de tokens de verificação de e-mail.
    /// </summary>
    public const long TABLE_EMAIL_VERIFICATION_TOKENS = 4;

    /// <summary>
    /// Versão de criação da tabela de refresh tokens.
    /// </summary>
    public const long TABLE_REFRESH_TOKENS = 5;
}
