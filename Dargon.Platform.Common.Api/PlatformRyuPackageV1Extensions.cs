using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Ryu;

namespace Dargon.Platform.Common {
   public static class PlatformRyuPackageV1Extensions {
      public static void LocalCorePlatformService<TInterface, TImplementation>(this RyuPackageV1 ryuPackage) where TImplementation : TInterface {
         LocalCorePlatformService<TInterface>(ryuPackage, ryu => ryu.ForceConstruct<TImplementation>());
      }

      public static void LocalCorePlatformService<TServiceInterface>(this RyuPackageV1 ryuPackage, Func<RyuContainer, TServiceInterface> factory) {
         ryuPackage.Singleton<TServiceInterface>(ryu => ConstructRegisterAndReturnPlatformService<TServiceInterface>(ryu, factory), RyuTypeFlags.Required);
      }

      private static TServiceInterface ConstructRegisterAndReturnPlatformService<TServiceInterface>(RyuContainer ryu, Func<RyuContainer, TServiceInterface> ctor) {
         var networkingResources = ryu.Get<PlatformNetworkingResources>();
         var service = ctor(ryu);
         networkingResources.LocalServiceClient.RegisterService(service, typeof(TServiceInterface));
         return service;
      }
   }
}
