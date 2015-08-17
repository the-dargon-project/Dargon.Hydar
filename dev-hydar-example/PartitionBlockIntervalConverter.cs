using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;
using ItzWarty.Collections;

namespace Dargon.Hydar {
   public interface PartitionBlockIntervalConverter {
      IUniqueIdentificationSet ConvertToUidSet(PartitionBlockInterval[] intervals);
      PartitionBlockInterval[] ConvertToPartitionBlockIntervals(IUniqueIdentificationSet set);
   }

   public class PartitionBlockIntervalConverterImpl : PartitionBlockIntervalConverter {
      public IUniqueIdentificationSet ConvertToUidSet(PartitionBlockInterval[] intervals) {
         var segments = new LinkedList<UniqueIdentificationSet.Segment>();
         foreach (var interval in intervals) {
            segments.AddLast(new UniqueIdentificationSet.Segment { low = interval.StartBlockInclusive, high = interval.EndBlockExclusive - 1 });
         }

         var result = new UniqueIdentificationSet(false);
         result.__Assign(segments);
         return result;
      }

      public PartitionBlockInterval[] ConvertToPartitionBlockIntervals(IUniqueIdentificationSet set) {
         PartitionBlockInterval[] intervals = null;
         set.__Access(segments => {
            intervals = segments.Select(segment => new PartitionBlockInterval(segment.low, segment.high + 1)).ToArray();
         });
         return intervals;
      }
   }
}