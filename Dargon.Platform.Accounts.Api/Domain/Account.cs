using System;

namespace Dargon.Platform.Accounts.Domain {
   public class Account {
      public Guid Id { get; set; }
      public string Username { get; set; }
      public string Password { get; set; }
      public DateTime Created { get; set; }
      public DateTime Modified { get; set; }
      public DateTime LastLogin { get; set; }
   }
}
