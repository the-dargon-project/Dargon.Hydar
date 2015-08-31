using System;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache.PortableObjects {
   public class LeaderHeartbeatDto : HydarCacheMessageBase {
      public LeaderHeartbeatDto() : base(Guid.Empty) { }

      public LeaderHeartbeatDto(Guid cacheId, Guid epochId, Guid[] orderedParticipants) : base(cacheId) {
         this.EpochId = epochId;
         this.OrderedParticipants = orderedParticipants;
      }

      public Guid EpochId { get; set; }
      public Guid[] OrderedParticipants { get; set; }

      protected override void Serialize(IPofWriter writer, int baseSlot) {
         writer.WriteGuid(baseSlot + 0, EpochId);
         writer.WriteCollection(baseSlot + 1, OrderedParticipants);
      }

      protected override void Deserialize(IPofReader reader, int baseSlot) {
         EpochId = reader.ReadGuid(baseSlot + 0);
         OrderedParticipants = reader.ReadArray<Guid>(baseSlot + 1);
      }
   }
}