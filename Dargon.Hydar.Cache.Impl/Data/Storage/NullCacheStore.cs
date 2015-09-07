using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Hydar.Cache.Data.Storage {
   public class NullCacheStore<TKey, TValue> : CacheStore<TKey, TValue> {
      public bool TryGet(TKey key, out TValue value) {
         value = default(TValue);
         return false;
      }

      public void Delete(TKey key) { }

      public void Update(TKey key, TValue value) { }
   }
}
