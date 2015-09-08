using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Hydar.Cache.Data.Storage {
   public interface CacheStorageStrategyFactory {
      CacheStorageStrategy<TKey, TValue> Create<TKey, TValue>(CacheStore<TKey, TValue> cacheStore, CacheStorageStrategy cacheStorageStrategy);
   }

   public class CacheStorageStrategyFactoryImpl : CacheStorageStrategyFactory {
      public CacheStorageStrategy<TKey, TValue> Create<TKey, TValue>(CacheStore<TKey, TValue> cacheStore, CacheStorageStrategy cacheStorageStrategy) {
         switch (cacheStorageStrategy) {
            case CacheStorageStrategy.Default:
            case CacheStorageStrategy.WriteThrough:
               return new WriteThroughCacheStorageStrategyImpl<TKey, TValue>(cacheStore);
            default:
               throw new NotSupportedException("Unknown cache storage strategy: " + cacheStorageStrategy);
         }
      }
   }
}
