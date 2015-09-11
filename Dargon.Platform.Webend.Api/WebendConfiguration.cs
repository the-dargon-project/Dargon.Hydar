using System.Net;

namespace Dargon.Platform.Webend {
   public interface WebendConfiguration {
      IPEndPoint[] PlatformServiceEndpoints { get; }
   }

   public class WebendConfigurationImpl : WebendConfiguration {
      public IPEndPoint[] PlatformServiceEndpoints { get; set; }
   }
}
