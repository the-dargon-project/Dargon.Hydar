using System;

namespace Dargon.Platform.Accounts {
   public class AccountServiceProxyImpl : AccountService {
      private readonly AccountCreationService accountCreationService;

      public AccountServiceProxyImpl(AccountCreationService accountCreationService) {
         this.accountCreationService = accountCreationService;
      }

      public Guid CreateAccount(string username, string password) => accountCreationService.CreateAccount(username, password);
   }
}