using FluentMigrator;

namespace AuthCore.Infrastructure.Persistence.Migrations.Versions;

[Migration(DatabaseVersions.USERS_EMAIL_VERIFICATION, "Add email verification support to users table")]
public sealed class Version0000003 : ForwardOnlyMigration
{
    public override void Up()
    {
        Alter.Table("Users")
            .AddColumn("EmailVerifiedAt").AsDateTime().Nullable();
    }
}
