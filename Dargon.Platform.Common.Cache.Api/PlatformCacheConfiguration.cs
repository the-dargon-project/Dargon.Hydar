using MicroLite;

namespace Dargon.Platform.Common.Cache {
   public interface PlatformCacheConfiguration {
      ISessionFactory DatabaseSessionFactory { get; } 
   }
}