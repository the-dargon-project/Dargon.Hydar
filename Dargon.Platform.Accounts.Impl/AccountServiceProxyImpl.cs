using System;

namespace Dargon.Platform.Accounts {
   public class AccountServiceProxyImpl : AccountService {
      private readonly AccountCreationService accountCreationService;
      private readonly AccountAuthenticationService accountAuthenticationService;
      private readonly AccountLookupService accountLookupService;

      public AccountServiceProxyImpl(AccountCreationService accountCreationService, AccountAuthenticationService accountAuthenticationService, AccountLookupService accountLookupService) {
         this.accountCreationService = accountCreationService;
         this.accountAuthenticationService = accountAuthenticationService;
         this.accountLookupService = accountLookupService;
      }

      public Guid CreateAccount(string username, string saltedPassword) => accountCreationService.CreateAccount(username, saltedPassword);

      public bool TryAuthenticate(string username, string saltedPassword, out Guid accountId, out string accessToken) => accountAuthenticationService.TryAuthenticate(username, saltedPassword, out accountId, out accessToken);

      public bool TryValidateToken(string accessToken, out Guid accountId) => accountAuthenticationService.TryValidateToken(accessToken, out accountId);

      public bool TryGetAccountIdByUsername(string name, out Guid accountId) => accountLookupService.TryGetAccountIdByUsername(name, out accountId);
   }
}