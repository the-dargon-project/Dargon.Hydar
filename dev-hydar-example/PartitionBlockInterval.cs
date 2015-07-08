using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.PortableObjects;

namespace Dargon.Hydar {
   public class PartitionBlockInterval : IPortableObject {
      public PartitionBlockInterval() { }

      public PartitionBlockInterval(int startBlockInclusive, int endBlockExclusive) {
         this.StartBlockInclusive = startBlockInclusive;
         this.EndBlockExclusive = endBlockExclusive;
      }

      public int StartBlockInclusive { get; set; }
      public int EndBlockExclusive { get; set; }

      public override string ToString() => $"[{StartBlockInclusive},{EndBlockExclusive})";

      public void Serialize(IPofWriter writer) {
         writer.WriteS32(0, StartBlockInclusive);
         writer.WriteS32(1, EndBlockExclusive);
      }

      public void Deserialize(IPofReader reader) {
         StartBlockInclusive = reader.ReadS32(0);
         EndBlockExclusive = reader.ReadS32(1);
      }
   }
}
