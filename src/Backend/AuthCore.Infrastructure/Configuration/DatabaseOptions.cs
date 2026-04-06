namespace AuthCore.Infrastructure.Configuration;

/// <summary>
/// Represents database configuration settings.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "ConnectionStrings";

    /// <summary>
    /// The PostgreSQL connection string.
    /// </summary>
    public string PostgreSql { get; init; } = string.Empty;
}