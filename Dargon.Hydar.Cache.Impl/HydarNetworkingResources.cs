using Dargon.Courier;
using Dargon.Services;

namespace Dargon.Hydar.Cache {
   public interface HydarNetworkingResources {
      ServiceClient LocalServiceClient { get; }
      CourierClient LocalCourierClient { get; }
   }

   public class HydarNetworkingResourcesImpl : HydarNetworkingResources {
      public CourierClient LocalCourierClient { get; set; }
      public ServiceClient LocalServiceClient { get; set; }
   }
}