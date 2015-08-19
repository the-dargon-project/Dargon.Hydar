using System;
using System.Collections.Generic;
using Dargon.Hydar.Utilities;

namespace Dargon.Hydar {
   public class ClusterClient {
      private readonly List<object> keepalive = new List<object>();

      public void AddCache<TKey, TValue>(CacheRoot<TKey, TValue> cache) {
         keepalive.Add(cache);
      }
   }

   public class CacheConfiguration {
      public string Name { get; set; }

      /// <summary>
      /// Gets the GUID of the cache, which is equivalent to the MD5 of the cache's name.
      /// </summary>
      public Guid Guid { get; set; }
   }
}
