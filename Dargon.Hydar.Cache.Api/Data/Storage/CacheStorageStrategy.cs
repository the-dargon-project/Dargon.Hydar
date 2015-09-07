using Dargon.Hydar.Common;

namespace Dargon.Hydar.Cache.Data.Storage {
   public interface CacheStorageStrategy<TKey, TValue> {
      bool TryGet(TKey key, out TValue value);
      void Updated(Entry<TKey, TValue> entry);
   }
}