using System;
using MicroLite.Mapping;
using MicroLite.Mapping.Attributes;

namespace Dargon.Platform.Accounts.Domain {
   [Table("accounts")]
   public class Account {
      [Identifier(IdentifierStrategy.Assigned)]
      [Column("id")]
      public Guid Id { get; set; }

      [Column("username")]
      public string Username { get; set; }

      [Column("password")]
      public string Password { get; set; }

      [Column("created")]
      public DateTime Created { get; set; }

      [Column("modified")]
      public DateTime Modified { get; set; }

      [Column("last_login")]
      public DateTime LastLogin { get; set; }

      public override string ToString() => $"[Account Username={Username}]";
   }
}
