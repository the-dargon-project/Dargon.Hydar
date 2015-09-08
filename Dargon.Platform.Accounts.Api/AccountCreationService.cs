using System;

namespace Dargon.Platform.Accounts {
   public interface AccountCreationService {
      Guid CreateAccount(string username, string saltedPassword);
   }
}