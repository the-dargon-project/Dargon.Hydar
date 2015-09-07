using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Dargon.Hydar.Common;

namespace Dargon.Hydar.Cache {
   public interface CacheStore {
      Task<CacheStoreTryReadResult<TValue>> TryReadAsync<TKey, TValue>(TKey key);
      void TryUpdateAsync<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> entry);
   }

   public class CacheStoreTryReadResult<TValue> {
      public bool Success { get; set; }
      public TValue Value { get; set; }
   }
}
