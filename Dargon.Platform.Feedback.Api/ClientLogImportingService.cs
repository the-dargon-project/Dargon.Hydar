using System.Runtime.InteropServices;
using Dargon.Platform.Common;

namespace Dargon.Platform.Feedback {
   [Guid("BD39C46D-095F-4739-BF53-AF4CF4802A39")]
   public interface ClientLogImportingService {
      void ImportUserLogs(ClientDescriptor clientDescriptor, byte[] zipArchiveContents);
   }
}
