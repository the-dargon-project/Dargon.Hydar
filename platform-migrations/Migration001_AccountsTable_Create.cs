using FluentMigrator;

namespace Dargon.Platform.Migrations {
   [Migration(201509070438)]
   public class Migration001_AccountsTable_Create : Migration {
      public override void Up() {
         Create.Table("accounts")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("username").AsString(32)
            .WithColumn("password").AsString(64)
            .WithColumn("created").AsDateTimeOffset()
            .WithColumn("modified").AsDateTimeOffset()
            .WithColumn("last_login").AsDateTimeOffset();
      }

      public override void Down() {
         Delete.Table("accounts");
      }
   }
}
