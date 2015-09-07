using System;
using Dargon.Hydar.Cache;
using Dargon.Hydar.Client;
using Dargon.Platform.Accounts.Domain;

namespace Dargon.Platform.Accounts.Hydar {
   public class Caches {
      private readonly CacheFactory cacheFactory;
      private readonly CacheDispatcher cacheDispatcher;
      private CacheRoot<Guid, Account> accountCache;

      public Caches(CacheFactory cacheFactory, CacheDispatcher cacheDispatcher) {
         this.cacheFactory = cacheFactory;
         this.cacheDispatcher = cacheDispatcher;
      }

      public Cache<Guid, Account> Account => accountCache;

      public void Initialize() {
         accountCache = cacheFactory.Create(new CacheConfigurationImpl<Guid, Account> {
            Name = "account-cache"
         });
         cacheDispatcher.RegisterCache(accountCache);
      }
   }
}
