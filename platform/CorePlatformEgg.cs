using Dargon.Hydar.Cache;
using Dargon.Management.Server;
using Dargon.Nest.Egg;
using Dargon.Platform.Accounts;
using Dargon.Platform.Common;
using Dargon.Platform.Common.Cache;
using Dargon.Ryu;
using ItzWarty;
using ItzWarty.Networking;
using MicroLite.Configuration;
using System;
using System.Collections.Generic;
using CommandLine;

namespace Dargon.Hydar {
   public class CorePlatformEgg : INestApplicationEgg {
      private readonly List<object> keepalive = new List<object>();
      private readonly RyuContainer ryu;

      public CorePlatformEgg() {
         ryu = new RyuFactory().Create();
//         ((RyuContainerImpl)ryu).SetLoggerEnabled(true);
      }

      public NestResult Start(IEggParameters parameters) {
         var options = new CorePlatformOptions();
         Parser.Default.ParseArguments(new string[0], options);

         return Start(options);
      }

      public NestResult Start(CorePlatformOptions corePlatformOptions) {
         ryu.Touch<ItzWartyCommonsRyuPackage>();
         ryu.Touch<ItzWartyProxiesRyuPackage>();

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

         ryu.Set<SystemState>(new PlatformSystemStateImpl());

         // Dargon.Management
         var managementServerEndpoint = ryu.Get<INetworkingProxy>().CreateAnyEndPoint(corePlatformOptions.ManagementPort);
         ryu.Set<IManagementServerConfiguration>(new ManagementServerConfiguration(managementServerEndpoint));

         // Initialize Hydar Cache
         ((RyuContainerImpl)ryu).Setup(true);

         Console.WriteLine("Initialized!");

         return NestResult.Success;
      }

      public NestResult Shutdown() {
         return NestResult.Success;
      }
   }
}