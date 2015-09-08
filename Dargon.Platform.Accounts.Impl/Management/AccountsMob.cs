using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Management;

namespace Dargon.Platform.Accounts.Management {
   public class AccountsMob {
      private readonly AccountService accountService;

      public AccountsMob(AccountService accountService) {
         this.accountService = accountService;
      }

      [ManagedOperation]
      public string CreateAccount(string username, string password) {
         var accountId = accountService.CreateAccount(username, password);
         return "Created account: " + accountId;
      }

      [ManagedOperation]
      public bool TryAuthenticateSalted(string username, string saltedPassword) {
         return accountService.TryAuthenticate(username, saltedPassword);
      }

      [ManagedOperation]
      public Guid LookupAccountId(string username) {
         Guid accountId;
         if (accountService.TryGetAccountIdByUsername(username, out accountId)) {
            return accountId;
         } else {
            return Guid.Empty;
         }
      }
   }
}
