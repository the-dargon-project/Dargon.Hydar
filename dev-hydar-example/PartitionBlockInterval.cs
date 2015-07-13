using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.PortableObjects;
using ItzWarty;
using ItzWarty.Collections;

namespace Dargon.Hydar {
   public class PartitionBlockInterval : IPortableObject {
      public PartitionBlockInterval() { }

      public PartitionBlockInterval(uint startBlockInclusive, uint endBlockExclusive) {
         this.StartBlockInclusive = startBlockInclusive;
         this.EndBlockExclusive = endBlockExclusive;
      }

      public uint StartBlockInclusive { get; set; }
      public uint EndBlockExclusive { get; set; }

      public override string ToString() => $"[{StartBlockInclusive},{EndBlockExclusive})";

      public void Serialize(IPofWriter writer) {
         writer.WriteU32(0, StartBlockInclusive);
         writer.WriteU32(1, EndBlockExclusive);
      }

      public void Deserialize(IPofReader reader) {
         StartBlockInclusive = reader.ReadU32(0);
         EndBlockExclusive = reader.ReadU32(1);
      }

      public static IUniqueIdentificationSet ToUidSet(PartitionBlockInterval[] input) {
         return new UniqueIdentificationSet(false).With(x => {
            x.__Assign(new LinkedList<UniqueIdentificationSet.Segment>(
               Util.Generate(input.Length, i => new UniqueIdentificationSet.Segment {
                  low = input[i].StartBlockInclusive,
                  high = input[i].EndBlockExclusive - 1
               })
            ));
         });
      }
   }
}
