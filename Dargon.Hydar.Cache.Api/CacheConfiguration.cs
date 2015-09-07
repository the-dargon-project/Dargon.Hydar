using System;
using Dargon.Hydar.Cache.Data.Storage;

namespace Dargon.Hydar.Cache {
   public interface CacheConfiguration<TKey, TValue> {
      string Name { get; set; }

      /// <summary>
      /// Gets the GUID of the cache, which is equivalent to the MD5 of the cache's name.
      /// </summary>
      Guid Guid { get; set; }

      int ServicePort { get; set; }

      CacheStore<TKey, TValue> CacheStore { get; set; }
   }

   public class CacheConfigurationImpl<TKey, TValue> : CacheConfiguration<TKey, TValue> {
      public string Name { get; set; }
      public Guid Guid { get; set; }
      public int ServicePort { get; set; }
      public CacheStore<TKey, TValue> CacheStore { get; set; }
   }
}