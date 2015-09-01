using Dargon.Hydar.Client;

namespace Dargon.Hydar.Cache {
   public interface CacheFactory {
      CacheRoot<TKey, TValue> Create<TKey, TValue>(string cacheName);
   }
}