using System;
using Dargon.Courier;
using Dargon.Courier.Messaging;
using Dargon.Hydar.Cache.Data.Storage;
using Dargon.Hydar.Common.Utilities;
using Dargon.Management.Server;
using Dargon.PortableObjects;
using Dargon.Ryu;
using Dargon.Services;

namespace Dargon.Hydar.Cache {
   public class HydarRyuPackage : RyuPackageV1 {
      public HydarRyuPackage() {
         Singleton<HydarNetworkingResources>(ConstructHydarNetworkingResources);
         Singleton<CacheFactory>(ConstructCacheFactory);
         Singleton<CacheDispatcher>(ConstructCacheDispatcher);
         Singleton<CacheStorageStrategyFactory, CacheStorageStrategyFactoryImpl>();
         Singleton<CacheInitializerFacade, CacheInitializerFacadeImpl>();

         PofContext<HydarCachePofContext>();
      }

      private CacheFactory ConstructCacheFactory(RyuContainer ryu) {
         var networkingResources = ryu.Get<HydarNetworkingResources>();
         return new CacheFactoryImpl(
            ryu.Get<GuidHelper>(),
            ryu.Get<IServiceClientFactory>(),
            networkingResources.LocalServiceClient,
            networkingResources.LocalCourierClient,
            ryu.Get<ILocalManagementServer>(),
            ryu.Get<ReceivedMessageFactory>(),
            ryu.Get<IPofContext>()
         );
      }

      private CacheDispatcher ConstructCacheDispatcher(RyuContainer ryu) {
         var networkingResources = ryu.Get<HydarNetworkingResources>();
         MessageRouter messageRouter = networkingResources.LocalCourierClient;
         return new CacheDispatcherImpl(messageRouter);
      }

      private HydarNetworkingResources ConstructHydarNetworkingResources(RyuContainer ryu) {
         var hydarConfiguration = ryu.Get<HydarConfiguration>();

         // Initialize Dargon.Services
         var clusteringConfiguration = new ClusteringConfiguration(hydarConfiguration.ServicePort, 1000, ClusteringRoleFlags.HostOnly);
         var serviceClientFactory = ryu.Get<IServiceClientFactory>();
         var serviceClient = serviceClientFactory.CreateOrJoin(clusteringConfiguration);

         // Initialize Dargon.Courier
         var courierClientFactory = ryu.Get<CourierClientFactory>();
         var courierClient = courierClientFactory.CreateUdpCourierClient(hydarConfiguration.CourierPort);

         return new HydarNetworkingResourcesImpl {
            LocalServiceClient = serviceClient,
            LocalCourierClient = courierClient
         };
      }
   }
}
