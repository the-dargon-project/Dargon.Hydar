using Dargon.Hydar.Common;

namespace Dargon.Hydar.Cache.Data.Storage {
   public class WriteThroughCacheStorageStrategyImpl<TKey, TValue> : CacheStorageStrategy<TKey, TValue> {
      private readonly CacheStore<TKey, TValue> cacheStore;

      public WriteThroughCacheStorageStrategyImpl(CacheStore<TKey, TValue> cacheStore) {
         this.cacheStore = cacheStore;
      }

      public bool TryGet(TKey key, out TValue value) {
         return cacheStore.TryGet(key, out value);
      }

      public void Updated(Entry<TKey, TValue> entry) {
         if (entry.IsDeleted) {
            cacheStore.Delete(entry.Key);
         } else {
            cacheStore.Update(entry.Key, entry.Value);
         }
      }
   }
}