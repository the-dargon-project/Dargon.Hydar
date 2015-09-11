using System.Net;
using Castle.DynamicProxy;
using Dargon.Platform.Accounts;
using Dargon.Ryu;
using Dargon.Services;
using Dargon.Services.Clustering;
using ItzWarty.Collections;

namespace Dargon.Platform.Webend {
   public class WebendRyuPackage : RyuPackageV1 {
      public WebendRyuPackage() {
         Singleton<WebendNetworkingResources>(ConstructWebendServiceClients);
         RemotePlatformService<AccountService>();
      }

      private void RemotePlatformService<TService>() where TService : class {
         Singleton<TService>(ryu => {
            var networkingResources = ryu.Get<WebendNetworkingResources>();
            return networkingResources.CorePlatform.GetService<TService>();
         }, RyuTypeFlags.Required);
      }

      private WebendNetworkingResources ConstructWebendServiceClients(RyuContainer ryu) {
         var configuration = ryu.Get<WebendConfiguration>();
         var serviceClientFactory = ryu.Get<IServiceClientFactory>();
         
         var initialRemoteServiceClientsByIpEndpoint = new ConcurrentDictionary<IPEndPoint, IServiceClient>();
         foreach (var platformServiceEndpoint in configuration.PlatformServiceEndpoints) {
            initialRemoteServiceClientsByIpEndpoint.GetOrAdd(
               platformServiceEndpoint,
               add => serviceClientFactory.CreateOrJoin(new ClusteringConfiguration(platformServiceEndpoint.Address, platformServiceEndpoint.Port, ClusteringRoleFlags.GuestOnly)));
         }

         var container = new RemoteClusterServiceProxyContainerImpl(
            ryu.Get<ProxyGenerator>(),
            initialRemoteServiceClientsByIpEndpoint);
         var serviceClient = new RemoteClusterServiceClientImpl(container);

         return new WebendNetworkingResourcesImpl {
            CorePlatform = serviceClient
         };
      }
   }
}
