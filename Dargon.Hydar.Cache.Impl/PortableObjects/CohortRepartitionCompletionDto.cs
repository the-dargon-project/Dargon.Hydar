using System;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache.PortableObjects {
   public class CohortRepartitionCompletionDto : HydarCacheMessageBase {
      public CohortRepartitionCompletionDto() : base(Guid.Empty) { }

      public CohortRepartitionCompletionDto(Guid cacheId) : base(cacheId) { }

      protected override void Serialize(IPofWriter writer, int baseSlot) { }

      protected override void Deserialize(IPofReader reader, int baseSlot) { }
   }
}