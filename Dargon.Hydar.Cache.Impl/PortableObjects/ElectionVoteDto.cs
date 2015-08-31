using System;
using Dargon.PortableObjects;
using ItzWarty.Collections;

namespace Dargon.Hydar.Cache.PortableObjects {
   public class ElectionVoteDto : HydarCacheMessageBase {
      public ElectionVoteDto() : base(Guid.Empty) { }

      public ElectionVoteDto(Guid cacheId, Guid nominee, IReadOnlySet<Guid> followers) : base(cacheId) {
         this.Nominee = nominee;
         this.Followers = followers;
      }

      public Guid Nominee { get; set; }
      public IReadOnlySet<Guid> Followers { get; set; }

      protected override void Serialize(IPofWriter writer, int baseSlot) {
         writer.WriteGuid(baseSlot + 0, Nominee);
         writer.WriteCollection(baseSlot + 1, Followers ?? new ItzWarty.Collections.HashSet<Guid>());
      }

      protected override void Deserialize(IPofReader reader, int baseSlot) {
         Nominee = reader.ReadGuid(baseSlot + 0);
         Followers = reader.ReadCollection<Guid, ItzWarty.Collections.HashSet<Guid>>(baseSlot + 1);
      }
   }
}