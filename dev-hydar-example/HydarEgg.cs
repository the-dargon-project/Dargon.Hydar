using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Dargon.Courier;
using Dargon.Courier.Messaging;
using Dargon.Hydar.Cache;
using Dargon.Hydar.Cache.Messaging;
using Dargon.Management;
using Dargon.Management.Server;
using Dargon.Nest.Egg;
using Dargon.Platform.Accounts;
using Dargon.Platform.Common.Cache;
using Dargon.PortableObjects;
using Dargon.Ryu;
using Dargon.Services;
using Dargon.Zilean.Client;
using ItzWarty;
using ItzWarty.Networking;
using MicroLite.Configuration;
using NLog;

namespace Dargon.Hydar {
   public class HydarEgg : INestApplicationEgg {
      private readonly List<object> keepalive = new List<object>();
      private readonly RyuContainer ryu;

      public HydarEgg() {
         ryu = new RyuFactory().Create();
//         ((RyuContainerImpl)ryu).SetLoggerEnabled(true);
      }

      public NestResult Start(IEggParameters parameters) {
         throw new NotImplementedException();
      }

      public NestResult Start(int servicePort, int managementPort, string connectionString) {
         Configure.Extensions().WithAttributeBasedMapping();

         ryu.Set<PlatformCacheConfiguration>(new PlatformCacheConfigurationImpl {
            DatabaseSessionFactory = Configure.Fluently()
               .ForPostgreSqlConnection("Dargon", connectionString, "Npgsql")
               .CreateSessionFactory()
         });

         ryu.Touch<ItzWartyProxiesRyuPackage>();

         // Dargon.Management
         var managementServerEndpoint = ryu.Get<INetworkingProxy>().CreateAnyEndPoint(managementPort);
         ryu.Set<IManagementServerConfiguration>(new ManagementServerConfiguration(managementServerEndpoint));

         // Dargon.Services for node-to-node networking
         ryu.Touch<ServicesRyuPackage>();
         var clusteringConfiguration = new ClusteringConfiguration(servicePort, 1000, ClusteringRoleFlags.HostOnly);
         ryu.Set<IClusteringConfiguration>(clusteringConfiguration);
         var serviceClient = ryu.Get<IServiceClient>();
         keepalive.Add(serviceClient);

         // Initialize Dargon.Courier
         var courierPort = 50555;
         var courierClientFactory = ryu.Get<CourierClientFactory>();
         var courierClient = courierClientFactory.CreateUdpCourierClient(courierPort);
         ryu.Set<CourierClient>(courierClient);
         ryu.Set<MessageRouter>(courierClient);

         // Initialize Hydar Cache
         ryu.Touch<HydarRyuPackage>();
         var cacheInitializer = ryu.Get<CacheInitializerFacade>();
         ((CacheInitializerFacadeImpl)cacheInitializer).SetServicePort(servicePort);

         ryu.Set<SystemState>(new PlatformSystemStateImpl());
         ryu.Setup();
         ryu.Touch<ZileanClientApiRyuPackage>();
         ryu.Touch<DargonPlatformAccountsImplRyuPackage>();

         Console.WriteLine("Initialized!");

         var accountService = ryu.Get<AccountService>();
         
         var authenticationResult = accountService.TryAuthenticate("Warty", "test");
         Console.WriteLine("Authentication result: " + authenticationResult);
//         var accountId = accountService.CreateAccount("Warty", "test");
//         Console.WriteLine("Created account " + accountId);

         return NestResult.Success;
      }

      public NestResult Shutdown() {
         return NestResult.Success;
      }
   }
}