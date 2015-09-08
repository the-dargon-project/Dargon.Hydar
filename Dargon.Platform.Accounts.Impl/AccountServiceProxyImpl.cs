using System;

namespace Dargon.Platform.Accounts {
   public class AccountServiceProxyImpl : AccountService {
      private readonly AccountCreationService accountCreationService;
      private readonly AccountAuthenticationService accountAuthenticationService;

      public AccountServiceProxyImpl(AccountCreationService accountCreationService, AccountAuthenticationService accountAuthenticationService) {
         this.accountCreationService = accountCreationService;
         this.accountAuthenticationService = accountAuthenticationService;
      }

      public Guid CreateAccount(string username, string saltedPassword) => accountCreationService.CreateAccount(username, saltedPassword);

      public bool TryAuthenticate(string username, string saltedPassword) => accountAuthenticationService.TryAuthenticate(username, saltedPassword);
   }
}