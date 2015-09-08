using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Platform.Accounts.Management {
   public class AccountsMob {
      private readonly AccountService accountService;

      public AccountsMob(AccountService accountService) {
         this.accountService = accountService;
      }

      public string CreateAccount(string username, string password) {
         var accountId = accountService.CreateAccount(username, password);
         return "Created account: " + accountId;
      }
   }
}
