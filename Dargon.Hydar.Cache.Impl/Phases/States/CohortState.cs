using Dargon.Hydar.Cache.Data.Partitioning;
using Dargon.Hydar.Cache.PortableObjects.Helpers;

namespace Dargon.Hydar.Cache.Phases.States {
   public class CohortState<TKey, TValue> : EpochState<TKey, TValue> {
      public EntryBlockTable<TKey, TValue> BlockTable { get; set; }
      public PartitionBlockIntervalConverter IntervalConverter { get; set; }
   }
}
