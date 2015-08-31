using System;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache.PortableObjects {
   public class LeaderRepartitionCompletingDto : HydarCacheMessageBase {
      public LeaderRepartitionCompletingDto() : base(Guid.Empty) { }

      public LeaderRepartitionCompletingDto(Guid cacheId) : base(cacheId) { }

      protected override void Serialize(IPofWriter writer, int baseSlot) { }

      protected override void Deserialize(IPofReader reader, int baseSlot) { }
   }
}