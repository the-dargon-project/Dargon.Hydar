using System;
using Dargon.Hydar.Cache;
using Dargon.Hydar.Cache.Data.Storage;
using Dargon.Platform.Accounts.Domain;
using Dargon.Platform.Accounts.Hydar.Processors;
using Dargon.Platform.Common.Cache;
using Dargon.Zilean;

namespace Dargon.Platform.Accounts {
   public class AccountCreationServiceImpl : AccountCreationService {
      private readonly ChronokeeperService chronokeeperService;
      private readonly CacheStore<Guid, Account> accountCacheStore;

      public AccountCreationServiceImpl(ChronokeeperService chronokeeperService, CacheStore<Guid, Account> accountCacheStore) {
         this.chronokeeperService = chronokeeperService;
         this.accountCacheStore = accountCacheStore;
      }

      public Guid CreateAccount(string username, string saltedPassword) {
         var accountId = chronokeeperService.GenerateSequentialGuid();
         var account = new Account {
            Id = accountId,
            Username = username,
            Password = saltedPassword,
            Created = DateTime.UtcNow
         };
         try {
            accountCacheStore.Insert(accountId, account);
            return accountId;
         } catch (Exception) {
            throw new UsernameTakenException();
         }
      }
   }
}