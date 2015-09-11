using FluentMigrator;

namespace Dargon.Platform.Migrations {
   [Migration(201509111145)]
   public class Migration003_AccountsTable_UsernameCaseInsensitiveUnique : Migration {
      public override void Up() {
         Delete.Index("IX_accounts_username").OnTable("accounts");
         Execute.Sql("CREATE UNIQUE INDEX IX_accounts_username ON accounts(LOWER(username))");
      }

      public override void Down() {
         Delete.Index("IX_accounts_username").OnTable("accounts");
         Alter.Column("username").OnTable("accounts").AsString(32).Unique();
      }
   }
}