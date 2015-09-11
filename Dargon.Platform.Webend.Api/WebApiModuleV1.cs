using System;
using System.Threading;
using System.Threading.Tasks;
using Nancy;

namespace Dargon.Platform.Webend {
   public abstract class WebApiModuleV1 : NancyModule {
      private const string kApiVersion = "v1";
      private const string kApiBasePath = "api/" + kApiVersion;

      protected WebApiModuleV1(string pathOffset = null) : base(BuildPath(kApiBasePath, pathOffset)) { }

      public void Initialize() {
         SetupRoutes();
      }

      protected abstract void SetupRoutes();

      protected Func<object, CancellationToken, Task<dynamic>> ProxyAsJson<TResponse>(Func<object, CancellationToken, Task<TResponse>> func) {
         return async (parameters, token) => Response.AsJson(await func(parameters, token));
      }

      private static string BuildPath(string apiBasePath, string pathOffset) {
         if (string.IsNullOrWhiteSpace(pathOffset)) {
            return apiBasePath;
         } else {
            return apiBasePath + "/" + pathOffset;
         }
      }
   }
}