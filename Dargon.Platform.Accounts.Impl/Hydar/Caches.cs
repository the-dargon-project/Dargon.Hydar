using System;
using Dargon.Hydar.Cache;
using Dargon.Hydar.Cache.Data.Storage.MicroLite;
using Dargon.Platform.Accounts.Domain;
using Dargon.Platform.Common.Cache;

namespace Dargon.Platform.Accounts.Hydar {
   public class Caches {
      private readonly PlatformCacheConfiguration platformCacheConfiguration;
      private readonly CacheInitializerFacade cacheInitializer;
      private Cache<Guid, Account> accountCache;

      public Caches(PlatformCacheConfiguration platformCacheConfiguration, CacheInitializerFacade cacheInitializer) {
         this.platformCacheConfiguration = platformCacheConfiguration;
         this.cacheInitializer = cacheInitializer;
      }

      public Cache<Guid, Account> Account => accountCache;

      public void Initialize() {
         accountCache = cacheInitializer.NearCache(
            "account-cache", 
            new MicroLiteCacheStore<Guid, Account>(platformCacheConfiguration.DatabaseSessionFactory)
         );
      }
   }
}
