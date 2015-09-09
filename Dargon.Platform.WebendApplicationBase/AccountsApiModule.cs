using Nancy;
using System;
using System.Diagnostics;
using Dargon.Ryu;
using static Dargon.Platform.FrontendApplicationBase.WebendGlobals;

namespace Dargon.Platform.FrontendApplicationBase {
   public class AccountsApiModule : NancyModule {
      public AccountsApiModule(RyuContainer ryu) : base(kApiBasePath) {
         Get["/authenticate", runAsync: true] = async (_, cancellationToken) => {
            return Response.AsJson(new { Id = 20, Name = "Dancing Penguins", Metadata = new { Author = "Fred", Created = DateTime.Now } });
         };
      }
   }
}
