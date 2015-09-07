using Dargon.Hydar.Cache.Data.Entries;
using Dargon.Hydar.Cache.Data.Storage;
using ItzWarty;
using ItzWarty.Collections;

namespace Dargon.Hydar.Cache.Data.Partitioning {
   public class Block<TKey, TValue> {
      private readonly IConcurrentDictionary<TKey, CacheEntryContext<TKey, TValue>> entryContextsByKey = new ConcurrentDictionary<TKey, CacheEntryContext<TKey, TValue>>();
      private readonly CacheStorageStrategy<TKey, TValue> cacheStorageStrategy;

      public Block(int id, CacheStorageStrategy<TKey, TValue> cacheStorageStrategy) {
         Id = id;
         this.cacheStorageStrategy = cacheStorageStrategy;
      }

      public int Id { get; set; }
      public bool IsUpToDate { get; set; }

      public void BlahBlahEmpty() {
         IsUpToDate = true;
      }

      public void BlahBlahStale() {
         IsUpToDate = false;
      }

      public CacheEntryContext<TKey, TValue> GetEntry(TKey key) {
         return entryContextsByKey.GetOrAdd(
            key,
            new CacheEntryContext<TKey, TValue>(key, cacheStorageStrategy).With(x => x.Initialize())
         );
      }
   }
}