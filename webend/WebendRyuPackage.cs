using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Dargon.Platform.Webend;
using Dargon.Ryu;
using Dargon.Services;
using Dargon.Services.Clustering;
using ItzWarty.Collections;

namespace Dargon.Platform.FrontendApplicationBase {
   public class WebendRyuPackage : RyuPackageV1 {
      public WebendRyuPackage() {
         Singleton<WebendRemoteServiceClients>(ConstructWebendServiceClients);
      }

      private WebendRemoteServiceClients ConstructWebendServiceClients(RyuContainer ryu) {
         var serviceClientFactory = ryu.Get<IServiceClientFactory>();
         var container = new RemoteClusterServiceProxyContainerImpl(
            ryu.Get<ProxyGenerator>(),
            new ConcurrentDictionary<IPEndPoint, IServiceClient>());
         var serviceClient = new RemoteClusterServiceClientImpl(container);
         return new WebendRemoteServiceClientsImpl {
            CorePlatform = serviceClient
         };
      }
   }
}
