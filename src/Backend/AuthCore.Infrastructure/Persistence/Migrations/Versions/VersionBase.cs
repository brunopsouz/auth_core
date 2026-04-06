using FluentMigrator;
using FluentMigrator.Builders.Create.Table;

namespace AuthCore.Infrastructure.Persistence.Migrations.Versions;

/// <summary>
/// Base helper for versioned schema migrations.
/// </summary>
public abstract class VersionBase : ForwardOnlyMigration
{
    /// <summary>
    /// Creates a standard table with the shared columns used by aggregate roots.
    /// </summary>
    /// <param name="table">The table name.</param>
    protected ICreateTableColumnOptionOrWithColumnSyntax CreateTable(string table)
    {
        return Create.Table(table)
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
            .WithColumn("UpdateAt").AsDateTime().NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);
    }
}
