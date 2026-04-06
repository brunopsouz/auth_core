namespace AuthCore.Infrastructure.Persistence.Migrations.Versions;

/// <summary>
/// Stores ordered migration version numbers.
/// </summary>
public static class DatabaseVersions
{
    /// <summary>
    /// Creates the users table.
    /// </summary>
    public const long TABLE_USERS = 1;

    /// <summary>
    /// Creates the passwords table.
    /// </summary>
    public const long TABLE_PASSWORDS = 2;

    /// <summary>
    /// Adds email verification support to users.
    /// </summary>
    public const long USERS_EMAIL_VERIFICATION = 3;

    /// <summary>
    /// Creates the email verification tokens table.
    /// </summary>
    public const long TABLE_EMAIL_VERIFICATION_TOKENS = 4;
}
