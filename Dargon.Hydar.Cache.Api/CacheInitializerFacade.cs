using Dargon.Hydar.Cache.Data.Storage;

namespace Dargon.Hydar.Cache {
   public interface CacheInitializerFacade {
      Cache<TKey, TValue> NearCache<TKey, TValue>(string name, CacheStore<TKey, TValue> cacheStore = null, CacheStorageStrategy cacheStorageStrategy = CacheStorageStrategy.Default);
   }
}
