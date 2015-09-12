using System;
using MicroLite.Mapping;
using MicroLite.Mapping.Attributes;

namespace Dargon.Platform.Feedback {
   [Table("client_logs")]
   public class ClientLog {
      [Identifier(IdentifierStrategy.Assigned)]
      [Column("id")]
      public Guid Id { get; set; }

      [Column("batch_id")]
      public Guid BatchId { get; set; }

      [Column("filename")]
      public string FileName { get; set; }

      [Column("uploaded")]
      public DateTime Uploaded { get; set; }

      [Column("client_id")]
      public Guid ClientId { get; set; }

      [Column("client_name")]
      public string ClientName { get; set; }

      [Column("client_version")]
      public string ClientVersion { get; set; }

      [Column("contents")]
      public string Contents { get; set; }
   }
}