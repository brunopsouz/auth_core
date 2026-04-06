using System.ComponentModel.DataAnnotations;

namespace AuthCore.Infrastructure.Configuration;

/// <summary>
/// Controls schema creation and FluentMigrator execution.
/// </summary>
public sealed class DatabaseMigrationOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Database:Migrations";

    /// <summary>
    /// Indicates whether pending migrations should be applied during startup.
    /// </summary>
    public bool AutoMigrateOnStartup { get; set; } = false;

    /// <summary>
    /// Indicates whether the PostgreSQL database should be created automatically when missing.
    /// </summary>
    public bool EnsureDatabaseCreated { get; set; } = true;

    /// <summary>
    /// The maintenance database used to connect before creating the target database.
    /// </summary>
    [Required]
    public string AdminDatabase { get; set; } = "postgres";
}
