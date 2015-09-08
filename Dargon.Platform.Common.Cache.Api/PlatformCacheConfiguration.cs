using MicroLite;

namespace Dargon.Platform.Common.Cache {
   public interface PlatformCacheConfiguration {
      ISessionFactory DatabaseSessionFactory { get; } 
   }

   public class PlatformCacheConfigurationImpl : PlatformCacheConfiguration {
      public ISessionFactory DatabaseSessionFactory { get; set; }
   }
}