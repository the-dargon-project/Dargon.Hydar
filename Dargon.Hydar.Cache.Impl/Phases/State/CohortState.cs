using Dargon.Hydar.Cache.Data;

namespace Dargon.Hydar.Cache.Phases.State {
   public class CohortState<TKey, TValue> : EpochState<TKey, TValue> {
      public EntryBlockTable<TKey, TValue> BlockTable { get; set; }
      public PartitionBlockIntervalConverter IntervalConverter { get; set; }
   }
}
