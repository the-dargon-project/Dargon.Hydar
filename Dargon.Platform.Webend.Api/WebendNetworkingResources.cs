using Dargon.Services;

namespace Dargon.Platform.Webend {
   public interface WebendNetworkingResources {
      IServiceClient CorePlatform { get; }
   }

   public class WebendNetworkingResourcesImpl : WebendNetworkingResources {
      public IServiceClient CorePlatform { get; set; }
   }
}
