using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Ryu;
using Dargon.Services;

namespace Dargon.Platform.Common {
   public class DargonPlatformCommonApiRyuPackage : RyuPackageV1 {
      public DargonPlatformCommonApiRyuPackage() {
         Singleton<PlatformNetworkingResources>(ConstructPlatformNetworkingResources);
      }

      private PlatformNetworkingResources ConstructPlatformNetworkingResources(RyuContainer ryu) {
         var configuration = ryu.Get<PlatformConfiguration>();
         var serviceClientFactory = ryu.Get<ServiceClientFactory>();
         var serviceClient = serviceClientFactory.Local(configuration.ServicePort, ClusteringRole.HostOnly);
         return new PlatformNetworkingResourcesImpl {
            LocalServiceClient = serviceClient
         };
      }
   }
}
