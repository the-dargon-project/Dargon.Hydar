using System;
using Dargon.Hydar.Cache;
using Dargon.Hydar.Cache.Data.Storage.MicroLite;
using Dargon.Hydar.Client;
using Dargon.Platform.Accounts.Domain;
using Dargon.Platform.Common.Cache;

namespace Dargon.Platform.Accounts.Hydar {
   public class Caches {
      private readonly PlatformCacheConfiguration platformCacheConfiguration;
      private readonly CacheFactory cacheFactory;
      private readonly CacheDispatcher cacheDispatcher;
      private CacheRoot<Guid, Account> accountCache;

      public Caches(PlatformCacheConfiguration platformCacheConfiguration,  CacheFactory cacheFactory, CacheDispatcher cacheDispatcher) {
         this.platformCacheConfiguration = platformCacheConfiguration;
         this.cacheFactory = cacheFactory;
         this.cacheDispatcher = cacheDispatcher;
      }

      public Cache<Guid, Account> Account => accountCache;

      public void Initialize() {
         accountCache = cacheFactory.Create(new CacheConfigurationImpl<Guid, Account> {
            Name = "account-cache",
            CacheStore = new MicroLiteCacheStore<Guid, Account>(platformCacheConfiguration.DatabaseSessionFactory)
         });
         cacheDispatcher.RegisterCache(accountCache);
      }
   }
}
