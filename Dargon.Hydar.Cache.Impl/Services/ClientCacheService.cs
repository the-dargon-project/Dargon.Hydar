using Dargon.Hydar.Cache.PortableObjects;
using Dargon.Hydar.Common;

namespace Dargon.Hydar.Cache.Services {
   public interface ClientCacheService<TKey, TValue> {
      EntryTryGetResult<TValue> TryGet(TKey key);
      TValue Get(TKey key);
      bool Put(TKey key, TValue value);
      TResult Process<TResult>(TKey key, EntryProcessor<TKey, TValue, TResult> entryProcessor);
   }
}