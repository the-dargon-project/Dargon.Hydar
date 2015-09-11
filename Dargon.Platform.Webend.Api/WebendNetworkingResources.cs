using Dargon.Services;

namespace Dargon.Platform.Webend {
   public interface WebendNetworkingResources {
      ServiceClient CorePlatform { get; }
   }

   public class WebendNetworkingResourcesImpl : WebendNetworkingResources {
      public ServiceClient CorePlatform { get; set; }
   }
}
