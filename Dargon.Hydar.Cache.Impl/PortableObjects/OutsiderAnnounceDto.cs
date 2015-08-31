using System;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache.PortableObjects {
   public class OutsiderAnnounceDto : HydarCacheMessageBase {
      public OutsiderAnnounceDto() : base(Guid.Empty) { }

      public OutsiderAnnounceDto(Guid cacheId) : base(cacheId) { }

      protected override void Serialize(IPofWriter writer, int baseSlot) { }

      protected override void Deserialize(IPofReader reader, int baseSlot) { }
   }
}