using FluentMigrator;

namespace Dargon.Platform.Migrations {
   [Migration(201509112358)]
   public class Migration004_ClientLogs_Create : Migration {
      public override void Up() {
         Create.Table("client_logs")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("batch_id").AsGuid().Indexed()
            .WithColumn("client_id").AsGuid().Indexed()
            .WithColumn("client_name").AsString().Indexed()
            .WithColumn("client_version").AsString()
            .WithColumn("uploaded").AsDateTimeOffset().Indexed()
            .WithColumn("filename").AsString().Indexed()
            .WithColumn("contents").AsString();;

         Create.Index().OnTable("client_logs")
            .OnColumn("client_name").Descending()
            .OnColumn("client_version").Descending();
      }

      public override void Down() {
         Delete.Table("client_logs");
      }
   }
}
