using Dargon.Hydar.Cache.Data.Storage;

namespace Dargon.Hydar.Cache {
   public interface CacheInitializerFacade {
      Cache<TKey, TValue> NearCache<TKey, TValue>(string name, CacheStore<TKey, TValue> cacheStore = null, CacheStorageStrategy cacheStorageStrategy = CacheStorageStrategy.Default);
   }

   public class CacheInitializerFacadeImpl : CacheInitializerFacade {
      private readonly CacheFactory cacheFactory;
      private readonly CacheDispatcher cacheDispatcher;

      public CacheInitializerFacadeImpl(CacheFactory cacheFactory, CacheDispatcher cacheDispatcher) {
         this.cacheFactory = cacheFactory;
         this.cacheDispatcher = cacheDispatcher;
      }

      public Cache<TKey, TValue> NearCache<TKey, TValue>(string name, CacheStore<TKey, TValue> cacheStore, CacheStorageStrategy cacheStorageStrategy) {
         var cacheConfiguration = new CacheConfigurationImpl<TKey, TValue> {
            Name = name,
            Storage = cacheStore,
            StorageStrategy = cacheStorageStrategy
         };
         var cacheRoot = cacheFactory.Create(cacheConfiguration);
         cacheDispatcher.RegisterCache(cacheRoot);
         return cacheRoot;
      }
   }
}
