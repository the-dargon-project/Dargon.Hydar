using FluentMigrator;

namespace Dargon.Platform.Migrations {
   [Migration(201509080040)]
   public class Migration002_AccountsTable_UsernameUnique : Migration {
      public override void Up() {
         Alter.Column("username").OnTable("accounts").AsString(32).Unique();
      }

      public override void Down() {
         Alter.Column("username").OnTable("accounts").AsString(32);
      }
   }
}
