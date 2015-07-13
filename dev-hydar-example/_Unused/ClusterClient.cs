using System;

namespace Dargon.Hydar {
   public class ClusterClient {
      private readonly Guid identity;

      public ClusterClient(Guid identity) {
         this.identity = identity;
      }

      public CacheContext<TKey, TValue> StartCache<TKey, TValue>(string cacheName) {
         throw new NotImplementedException();
      }
   }

   public class CacheConfiguration {
      public string Name { get; set; }
   }

   public class CacheContext<TKey, TValue> {
      private readonly CacheConfiguration configuration;

      public CacheContext(CacheConfiguration configuration) {
         this.configuration = configuration;
      }

      public string Name => configuration.Name;
   }
}
