using System;
using Dargon.Hydar.Cache;
using Dargon.Hydar.Cache.Data.Storage;
using Dargon.Hydar.Cache.Data.Storage.MicroLite;
using Dargon.Platform.Accounts.Domain;
using Dargon.Platform.Common.Cache;

namespace Dargon.Platform.Accounts.Hydar {
   public class Caches {
      private readonly PlatformCacheConfiguration platformCacheConfiguration;
      private readonly CacheInitializerFacade cacheInitializer;
      private CacheStore<Guid, Account> accountCacheStore;
      private Cache<Guid, Account> accountCache;
      private Cache<string, Guid> accountIdByUsernameCache;
      private Cache<Guid, Guid> accessTokenCache;

      public Caches(PlatformCacheConfiguration platformCacheConfiguration, CacheInitializerFacade cacheInitializer) {
         this.platformCacheConfiguration = platformCacheConfiguration;
         this.cacheInitializer = cacheInitializer;
      }

      public CacheStore<Guid, Account> AccountCacheStore => accountCacheStore;
      public Cache<Guid, Account> Account => accountCache;
      public Cache<string, Guid> AccountIdByUsernameCache => accountIdByUsernameCache;
      public Cache<Guid, Guid> AccessTokenCache => accessTokenCache;

      public void Initialize() {
         accountCacheStore = new MicroLiteCacheStore<Guid, Account>(platformCacheConfiguration.DatabaseSessionFactory);
         accountCache = cacheInitializer.NearCache("account-cache", accountCacheStore);
         accountIdByUsernameCache = cacheInitializer.NearCache<string, Guid>("account-id-by-username-cache");
         accessTokenCache = cacheInitializer.NearCache<Guid, Guid>("access-token-cache");
      }
   }
}
