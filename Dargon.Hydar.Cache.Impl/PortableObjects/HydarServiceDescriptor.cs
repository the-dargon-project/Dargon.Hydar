using System.Diagnostics;
using System.Runtime.InteropServices;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache.PortableObjects {
   [Guid("07B72FFE-828A-4A20-BA83-06B60D990B30")]
   public class HydarServiceDescriptor : IPortableObject {
      private const int kVersion = 0;

      public int ServicePort { get; set; }

      public void Serialize(IPofWriter writer) {
         writer.WriteS32(0, kVersion);
         writer.WriteS32(1, ServicePort);
      }

      public void Deserialize(IPofReader reader) {
         var version = reader.ReadS32(0);
         ServicePort = reader.ReadS32(1);

         Trace.Assert(version == kVersion, "version == kVersion");
      }
   }
}