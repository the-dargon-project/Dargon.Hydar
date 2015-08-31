using System;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache.PortableObjects {
   public class CacheHaveDto : HydarCacheMessageBase {
      public CacheHaveDto() : base(Guid.Empty) { }

      public CacheHaveDto(Guid cacheId, PartitionBlockInterval[] blocks, int servicePort) : base(cacheId) {
         Blocks = blocks;
         ServicePort = servicePort;
      }

      public PartitionBlockInterval[] Blocks { get; set; }
      public int ServicePort { get; set; }

      protected override void Serialize(IPofWriter writer, int baseSlot) {
         writer.WriteCollection(baseSlot + 0, Blocks);
         writer.WriteS32(baseSlot + 1, ServicePort);
      }

      protected override void Deserialize(IPofReader reader, int baseSlot) {
         Blocks = reader.ReadArray<PartitionBlockInterval>(baseSlot + 0);
         ServicePort = reader.ReadS32(baseSlot + 1);
      }
   }
}