using System;

namespace Dargon.Hydar.Cache {
   public class CacheConfiguration {
      public string Name { get; set; }

      /// <summary>
      /// Gets the GUID of the cache, which is equivalent to the MD5 of the cache's name.
      /// </summary>
      public Guid Guid { get; set; }

      public int ServicePort { get; set; }
   }
}