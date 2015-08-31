using System;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache.PortableObjects {
   public class LeaderRepartitionSignalDto : HydarCacheMessageBase {
      private Guid epochId;
      private Guid[] participantsOrdered;

      public LeaderRepartitionSignalDto() : base(Guid.Empty) { }

      public LeaderRepartitionSignalDto(Guid cacheId, Guid epochId, Guid[] participantsOrdered) : base(cacheId) {
         this.epochId = epochId;
         this.participantsOrdered = participantsOrdered;
      }

      public Guid EpochId => epochId;
      public Guid[] ParticipantsOrdered => participantsOrdered;

      protected override void Serialize(IPofWriter writer, int baseSlot) {
         writer.WriteGuid(baseSlot + 0, epochId);
         writer.WriteCollection(baseSlot + 1, participantsOrdered);
      }

      protected override void Deserialize(IPofReader reader, int baseSlot) {
         epochId = reader.ReadGuid(baseSlot + 0);
         participantsOrdered = reader.ReadArray<Guid>(baseSlot + 1);
      }
   }
}