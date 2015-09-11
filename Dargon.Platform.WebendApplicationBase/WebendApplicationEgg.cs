using Dargon.Nest.Egg;
using Nancy.Hosting.Self;
using System;
using System.Linq;
using Dargon.Ryu;
using ItzWarty;
using Nancy;
using Nancy.TinyIoc;

namespace Dargon.Platform.FrontendApplicationBase {
   public class WebendApplicationEgg : INestApplicationEgg {
      private readonly RyuContainer ryu;
      private IEggHost eggHost;
      private NancyHost nancyHost;

      public WebendApplicationEgg() {
         this.ryu = new RyuFactory().Create();
      }

      public NestResult Start(IEggParameters parameters) {
         throw new NotImplementedException();
      }

      public NestResult Start(string baseUrl) {
         ryu.Setup();
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
   }
}
