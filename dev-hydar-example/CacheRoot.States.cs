using ItzWarty.Collections;
using System;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
      public class EpochState {
         public Guid EpochId { get; set; }
         public Guid Leader { get; set; }

         /// <summary>This array is ordered.</summary>
         public Guid[] Participants { get; set; }

         public Keyspace Keyspace { get; set; }
      }

      public class LeaderState : EpochState {
         public SubPhaseHost SubPhaseHost { get; set; }
         public IReadOnlySet<Guid> PendingOutsiders { get; set; }
      }

      public class CohortState : EpochState {
         public EntryBlockTable BlockTable { get; set; }
         public PartitionBlockIntervalConverter IntervalConverter { get; set; }
      }
   }
}
