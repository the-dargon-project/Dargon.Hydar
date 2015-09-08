using System;
using Dargon.Hydar.Cache;
using Dargon.Platform.Accounts.Domain;
using Dargon.Platform.Accounts.Hydar.Processors;

namespace Dargon.Platform.Accounts {
   public class AccountAuthenticationServiceImpl : AccountAuthenticationService {
      private readonly Cache<Guid, Account> accountCache;
      private readonly AccountProcessorFactory accountProcessorFactory;
      private readonly AccountLookupService accountLookupService;

      public AccountAuthenticationServiceImpl(Cache<Guid, Account> accountCache, AccountProcessorFactory accountProcessorFactory, AccountLookupService accountLookupService) {
         this.accountCache = accountCache;
         this.accountProcessorFactory = accountProcessorFactory;
         this.accountLookupService = accountLookupService;
      }

      public bool TryAuthenticate(string username, string saltedPassword) {
         Guid accountId;
         if (!accountLookupService.TryGetAccountIdByUsername(username, out accountId)) {
            return false;
         } else {
            Console.WriteLine("TGAIBU: " + accountId);
            var authenticationResult = accountCache.Process(accountId, accountProcessorFactory.Authenticate(saltedPassword));
            return authenticationResult;
         }
      }
   }
}