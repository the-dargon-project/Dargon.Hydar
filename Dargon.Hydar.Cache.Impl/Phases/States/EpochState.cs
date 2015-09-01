using System;
using Dargon.Hydar.Cache.Data.Partitioning;

namespace Dargon.Hydar.Cache.Phases.States {
   public class EpochState<TKey, TValue> {
      public Guid EpochId { get; set; }
      public Guid Leader { get; set; }

      /// <summary>This array is ordered.</summary>
      public Guid[] Participants { get; set; }

      public Keyspace Keyspace { get; set; }
   }
}