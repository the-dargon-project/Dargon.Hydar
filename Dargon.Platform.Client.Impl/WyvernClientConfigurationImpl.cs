using System;

namespace Dargon.Platform.Client {
   public class WyvernClientConfigurationImpl : WyvernClientConfiguration {
      public string ApiBase { get; set; } = "http://localhost:1234";
      public string ApiV1Base => ApiBase + "/api/v1";
      public string AccessToken { get; set; }

      public Guid ClientId { get; set; }
      public string ClientName { get; set; }
      public string ClientVersion { get; set; }
   }
}
