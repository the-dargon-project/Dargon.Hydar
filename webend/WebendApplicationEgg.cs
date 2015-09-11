using Dargon.Nest.Egg;
using Dargon.Ryu;
using Nancy.Hosting.Self;
using System;
using System.IO;
using System.Reflection;

namespace Dargon.Platform.FrontendApplicationBase {
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

      public NestResult Start(string baseUrl) {
         ryu.Setup();
         ForceLoadDirectoryAssemblies(ryu);
         if (nancyHost == null) {
            var bootstrapper = new RyuNancyBootstrapper(ryu);
            nancyHost = new NancyHost(new Uri(baseUrl), bootstrapper);
         }
         nancyHost.Start();
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

      public NestResult Shutdown() {
         nancyHost.Stop();
         return NestResult.Success;
      }
   }
}
