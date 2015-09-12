using System;
using System.Data;
using Dargon.Hydar.Cache;
using Dargon.Hydar.Cache.Data.Storage;
using Dargon.Hydar.Cache.Data.Storage.MicroLite;
using Dargon.Platform.Common.Cache;

namespace Dargon.Platform.Feedback {
   public class Caches {
      private readonly PlatformCacheConfiguration platformCacheConfiguration;
      private readonly CacheInitializerFacade cacheInitializer;

      private CacheStore<Guid, ClientLog> clientLogCacheStore;

      public Caches(PlatformCacheConfiguration platformCacheConfiguration, CacheInitializerFacade cacheInitializer) {
         this.platformCacheConfiguration = platformCacheConfiguration;
         this.cacheInitializer = cacheInitializer;
      }

      public CacheStore<Guid, ClientLog> ClientLogCacheStore => clientLogCacheStore;

      public void Initialize() {
         clientLogCacheStore = new MicroLiteCacheStore<Guid, ClientLog>(platformCacheConfiguration.DatabaseSessionFactory);
      }
   }
}
