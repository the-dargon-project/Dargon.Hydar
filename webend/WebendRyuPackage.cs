using System.Net;
using Castle.DynamicProxy;
using Dargon.Platform.Accounts;
using Dargon.Platform.Feedback;
using Dargon.Ryu;
using Dargon.Services;
using Dargon.Services.Clustering;
using Dargon.Services.Clustering.Remote;
using ItzWarty;
using ItzWarty.Collections;

namespace Dargon.Platform.Webend {
   public class WebendRyuPackage : RyuPackageV1 {
      public WebendRyuPackage() {
         Singleton<WebendNetworkingResources>(ConstructWebendServiceClients);
         RemotePlatformService<AccountService>();
         RemotePlatformService<ClientLogImportingService>();
      }

      private void RemotePlatformService<TService>() where TService : class {
         Singleton<TService>(ryu => {
            var networkingResources = ryu.Get<WebendNetworkingResources>();
            return networkingResources.CorePlatform.GetService<TService>();
         }, RyuTypeFlags.Required);
      }

      private WebendNetworkingResources ConstructWebendServiceClients(RyuContainer ryu) {
         var proxyGenerator = ryu.Get<ProxyGenerator>();
         var configuration = ryu.Get<WebendConfiguration>();
         var serviceClientFactory = ryu.Get<ServiceClientFactory>();
         var container = new RemoteServiceClientContainerImpl(serviceClientFactory);
         configuration.PlatformServiceEndpoints.ForEach(container.AddEndPoint);
         var serviceClient = new ServiceClientProxyImpl(new InvalidLocalServiceRegistryImpl(), new LoadBalancedRemoteServiceProxyContainerImpl(proxyGenerator, container));

         return new WebendNetworkingResourcesImpl {
            CorePlatform = serviceClient
         };
      }
   }
}
