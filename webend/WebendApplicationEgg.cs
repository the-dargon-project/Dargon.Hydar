using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Dargon.Nest.Egg;
using Dargon.Platform.FrontendApplicationBase;
using Dargon.Ryu;
using ItzWarty;
using ItzWarty.Networking;
using Nancy.Hosting.Self;

namespace Dargon.Platform.Webend {
   public class WebendApplicationEgg : INestApplicationEgg {
      private readonly RyuContainer ryu;
      private NancyHost nancyHost;

      public WebendApplicationEgg() {
         this.ryu = new RyuFactory().Create();
         ((RyuContainerImpl)ryu).SetLoggerEnabled(true);
      }

      public NestResult Start(IEggParameters parameters) {
         throw new NotImplementedException();
      }

      public NestResult Start(string baseUrl, WebendOptions webendOptions) {
         ryu.Touch<ItzWartyCommonsRyuPackage>();
         ryu.Touch<ItzWartyProxiesRyuPackage>();
         ryu.Set<WebendConfiguration>(new WebendConfigurationImpl {
            PlatformServiceEndpoints = ParseIpEndpoints(webendOptions.PlatformServiceEndpoints)
         });
         ryu.Setup();
         ForceLoadDirectoryAssemblies(ryu);
         if (nancyHost == null) {
            var bootstrapper = new RyuNancyBootstrapper(ryu);
            nancyHost = new NancyHost(new Uri(baseUrl), bootstrapper);
         }
         nancyHost.Start();
         return NestResult.Success;
      }

      public NestResult Shutdown() {
         nancyHost.Stop();
         return NestResult.Success;
      }

      /// <summary>
      /// Assemblies seem to only be loaded when they are needed.
      /// This forces the assemblies to be loaded immediately, so that ryu can
      /// detect packages and nancy modules.
      /// </summary>
      private static void ForceLoadDirectoryAssemblies(RyuContainer ryu) {
         var assemblies = Directory.EnumerateFiles(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName, "*.dll", SearchOption.AllDirectories);
         foreach (var assemblyPath in assemblies) {
            try {
               var assembly = Assembly.LoadFrom(assemblyPath);
               Console.WriteLine("Force load: " + assemblyPath);
               ryu.Touch(assembly);
            } catch (Exception) { }
         }
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
