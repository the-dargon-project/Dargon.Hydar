using System;

namespace Dargon.Platform.Client {
   public class Program {
      public static void Main() {
         var configuration = new WyvernClientConfigurationImpl {
            ClientId = Guid.NewGuid(),
            ClientName = "dargon",
            ClientVersion = "0.0.0"
         };
         var client = new WyvernClientImpl(configuration);
         client.LogIn("warty", "test");
         client.UploadLogs(@"C:\Dargon\logs.zip");
      }
   }
   
}
