using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using CommandLine;
using Dargon.Nest.Egg;
using Dargon.Platform.FrontendApplicationBase;
using Dargon.Ryu;
using ItzWarty;
using ItzWarty.Networking;
using Nancy.Hosting.Self;

namespace Dargon.Platform.Webend {
   public class WebendApplicationEgg : INestApplicationEgg {
      private readonly RyuContainer ryu;
      private IEggHost eggHost;
      private NancyHost nancyHost;

      public WebendApplicationEgg() {
         this.ryu = new RyuFactory().Create();
         ((RyuContainerImpl)ryu).SetLoggerEnabled(true);
      }

      public NestResult Start(IEggParameters parameters) {
         this.eggHost = parameters.Host;

         var options = new WebendOptions();
         Parser.Default.ParseArguments(new string[0], options);
         return Start(options);
      }

      public NestResult Start(WebendOptions webendOptions) {
         ryu.Touch<ItzWartyCommonsRyuPackage>();
         ryu.Touch<ItzWartyProxiesRyuPackage>();
         ryu.Set<WebendConfiguration>(new WebendConfigurationImpl {
            PlatformServiceEndpoints = ParseIpEndpoints(webendOptions.PlatformServiceEndpoints)
         });
         ((RyuContainerImpl)ryu).Setup(true);
         if (nancyHost == null) {
            var bootstrapper = new RyuNancyBootstrapper(ryu);
            nancyHost = new NancyHost(new Uri(webendOptions.BaseUrl), bootstrapper);
         }
         nancyHost.Start();
         return NestResult.Success;
      }

      public NestResult Shutdown() {
         nancyHost.Stop();
         eggHost.Shutdown();
         return NestResult.Success;
      }

      private IPEndPoint[] ParseIpEndpoints(string input) {
         var inputParts = input.Split(';');
         return Util.Generate(
            inputParts.Length,
            i => {
               var hostPortString = inputParts[i];
               var host = hostPortString.Substring(0, hostPortString.IndexOf(':'));
               var port = int.Parse(hostPortString.Substring(hostPortString.IndexOf(':') + 1));
               var networkingProxy = ryu.Get<INetworkingProxy>();
               return networkingProxy.CreateEndPoint(host, port).ToIPEndPoint();
            });
      }
   }
}
