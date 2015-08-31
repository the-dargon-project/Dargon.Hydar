using System;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache.PortableObjects {
   public class CacheNeedDto : HydarCacheMessageBase {
      public CacheNeedDto() : base(Guid.Empty) { }

      public CacheNeedDto(Guid cacheId, PartitionBlockInterval[] blocks) : base(cacheId) {
         Blocks = blocks;
      }

      public PartitionBlockInterval[] Blocks { get; set; }

      protected override void Serialize(IPofWriter writer, int baseSlot) {
         writer.WriteCollection(baseSlot + 0, Blocks);
      }

      protected override void Deserialize(IPofReader reader, int baseSlot) {
         Blocks = reader.ReadArray<PartitionBlockInterval>(baseSlot + 0);
      }
   }
}