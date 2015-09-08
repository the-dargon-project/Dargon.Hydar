using System;
using Dargon.Hydar.Cache;
using Dargon.Platform.Accounts.Domain;
using Dargon.Platform.Accounts.Hydar.Processors;
using Dargon.Zilean;

namespace Dargon.Platform.Accounts {
   public class AccountCreationServiceImpl : AccountCreationService {
      private readonly ChronokeeperService chronokeeperService;
      private readonly Cache<Guid, Account> accountCache;
      private readonly AccountProcessorFactory accountProcessorFactory;

      public AccountCreationServiceImpl(ChronokeeperService chronokeeperService, Cache<Guid, Account> accountCache, AccountProcessorFactory accountProcessorFactory) {
         this.chronokeeperService = chronokeeperService;
         this.accountCache = accountCache;
         this.accountProcessorFactory = accountProcessorFactory;
      }

      public Guid CreateAccount(string username, string password) {
         var accountId = chronokeeperService.GenerateSequentialGuid();
         var accountCreationProcessor = accountProcessorFactory.CreateAccount(username, password);
         if (!accountCache.Process(accountId, accountCreationProcessor)) {
            throw new AccountIdTakenException();
         } else {
            return accountId;
         }
      }
   }
}