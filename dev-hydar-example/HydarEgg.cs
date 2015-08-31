using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Dargon.Courier;
using Dargon.Hydar.Cache;
using Dargon.Hydar.Cache.Messaging;
using Dargon.Management;
using Dargon.Management.Server;
using Dargon.Nest.Egg;
using Dargon.Ryu;
using Dargon.Services;
using ItzWarty.Networking;

namespace Dargon.Hydar {
   public class HydarEgg : INestApplicationEgg {
      private readonly List<object> keepalive = new List<object>();
      private readonly RyuContainer ryu;

      public HydarEgg() {
         ryu = new RyuFactory().Create();
      }

      public NestResult Start(IEggParameters parameters) {
         throw new NotImplementedException();
      }

      public NestResult Start(int servicePort, int managementPort) {
         ryu.Setup();

         var networkingProxy = ryu.Get<INetworkingProxy>();

         // Dargon.Management
         var managementServerEndpoint = networkingProxy.CreateAnyEndPoint(managementPort);
         var managementFactory = ryu.Get<ManagementFactoryImpl>();
         var localManagementServer = managementFactory.CreateServer(new ManagementServerConfiguration(managementServerEndpoint));
         ryu.Set<ILocalManagementServer>(localManagementServer);
         keepalive.Add(localManagementServer);

         // Dargon.Services for node-to-node networking
         var clusteringConfiguration = new ClusteringConfiguration(servicePort, 1000, ClusteringRoleFlags.HostOnly);
         ryu.Set<IClusteringConfiguration>(clusteringConfiguration);
         var serviceClient = ryu.Get<IServiceClient>();
         keepalive.Add(serviceClient);

         // Initialize Dargon.Courier
         var courierPort = 50555;
         var courierClientFactory = ryu.Get<CourierClientFactory>();
         var courierClient = courierClientFactory.CreateUdpCourierClient(courierPort);
         ryu.Set<CourierClient>(courierClient);
         
         // Dargon.Courier for clustered networking
         Console.Title = "PID " + Process.GetCurrentProcess().Id + ": " + courierClient.Identifier.ToString("N");

         // Initialize Hydar Cache
         var cacheFactory = ryu.Get<CacheFactory>();
         cacheFactory.SetServicePort(servicePort);
         var cacheDispatcher = new CacheDispatcher(courierClient.MessageRouter);
         cacheDispatcher.Initialize();
         cacheDispatcher.AddCache(cacheFactory.Create<int, int>("test-cache"));
         cacheDispatcher.AddCache(cacheFactory.Create<int, string>("test-string-cache"));

         new CountdownEvent(1).Wait();
         return NestResult.Success;
      }

      public NestResult Shutdown() {
         return NestResult.Success;
      }
   }
}