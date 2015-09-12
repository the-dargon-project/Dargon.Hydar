using System;

namespace Dargon.Platform.Client {
   public interface WyvernClientConfiguration {
      string ApiBase { get; }
      string ApiV1Base { get; }

      Guid ClientId { get; }
      string ClientName { get; }
      string ClientVersion { get; }
   }
}