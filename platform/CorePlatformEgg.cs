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
using Dargon.Platform.Common;
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
   public class CorePlatformEgg : INestApplicationEgg {
      private readonly List<object> keepalive = new List<object>();
      private readonly RyuContainer ryu;

      public CorePlatformEgg() {
         ryu = new RyuFactory().Create();
//         ((RyuContainerImpl)ryu).SetLoggerEnabled(true);
      }

      public NestResult Start(IEggParameters parameters) {
         throw new NotImplementedException();
      }

      public NestResult Start(CorePlatformOptions corePlatformOptions) {
         Configure.Extensions().WithAttributeBasedMapping();

         ryu.Set<HydarConfiguration>(new HydarConfigurationImpl {
            ServicePort = corePlatformOptions.HydarServicePort,
            CourierPort = corePlatformOptions.HydarCourierPort
         });

         ryu.Set<PlatformConfiguration>(new PlatformConfigurationImpl {
            ServicePort = corePlatformOptions.ServicePort
         });

         ryu.Set<PlatformCacheConfiguration>(new PlatformCacheConfigurationImpl {
            DatabaseSessionFactory = Configure.Fluently()
               .ForPostgreSqlConnection("Dargon", corePlatformOptions.ConnectionString, "Npgsql")
               .CreateSessionFactory()
         });

         ryu.Touch<ItzWartyProxiesRyuPackage>();

         // Dargon.Management
         var managementServerEndpoint = ryu.Get<INetworkingProxy>().CreateAnyEndPoint(corePlatformOptions.ManagementPort);
         ryu.Set<IManagementServerConfiguration>(new ManagementServerConfiguration(managementServerEndpoint));

         // Initialize Hydar Cache
         ryu.Touch<HydarRyuPackage>();
         var cacheInitializer = ryu.Get<CacheInitializerFacade>();

         ryu.Set<SystemState>(new PlatformSystemStateImpl());
         ryu.Setup();
         ryu.Touch<ZileanClientApiRyuPackage>();
         ryu.Touch<DargonPlatformAccountsImplRyuPackage>();

         Console.WriteLine("Initialized!");

         var accountService = ryu.Get<AccountService>();

         Guid accountId, accessToken;
         var authenticationResult = accountService.TryAuthenticate("Warty", "test", out accountId, out accessToken);
         Console.WriteLine("Authentication result: " + authenticationResult + " accountId: " + accountId + " accessToken: " + accessToken);
//         var accountId = accountService.CreateAccount("Warty", "test");
//         Console.WriteLine("Created account " + accountId);

         return NestResult.Success;
      }

      public NestResult Shutdown() {
         return NestResult.Success;
      }
   }
}