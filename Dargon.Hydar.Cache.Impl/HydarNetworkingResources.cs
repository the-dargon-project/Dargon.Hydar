using Dargon.Courier;
using Dargon.Services;

namespace Dargon.Hydar.Cache {
   public interface HydarNetworkingResources {
      IServiceClient LocalServiceClient { get; }
      CourierClient LocalCourierClient { get; }
   }

   public class HydarNetworkingResourcesImpl : HydarNetworkingResources {
      public CourierClient LocalCourierClient { get; set; }
      public IServiceClient LocalServiceClient { get; set; }
   }
}