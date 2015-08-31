using System;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache.PortableObjects {
   public class CohortHeartbeatDto : HydarCacheMessageBase {
      public CohortHeartbeatDto() : base(Guid.Empty) { }

      public CohortHeartbeatDto(Guid cacheId) : base(cacheId) { }

      protected override void Serialize(IPofWriter writer, int baseSlot) { }

      protected override void Deserialize(IPofReader reader, int baseSlot) { }
   }
}