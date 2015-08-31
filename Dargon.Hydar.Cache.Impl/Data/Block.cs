using ItzWarty;
using ItzWarty.Collections;

namespace Dargon.Hydar.Cache.Data {
   public class Block<TKey, TValue> {
      private readonly IConcurrentDictionary<TKey, CacheEntryContext<TKey, TValue>> entryContextsByKey = new ConcurrentDictionary<TKey, CacheEntryContext<TKey, TValue>>();

      public Block(int id) {
         Id = id;
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
            new CacheEntryContext<TKey, TValue>(key).With(x => x.Initialize())
            );
      }
   }
}