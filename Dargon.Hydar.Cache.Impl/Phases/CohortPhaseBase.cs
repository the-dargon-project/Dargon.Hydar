using Dargon.Hydar.Cache.Data.Partitioning;
using Dargon.Hydar.Cache.Phases.States;
using Dargon.Hydar.Cache.PortableObjects;
using Dargon.Hydar.Cache.PortableObjects.Helpers;

namespace Dargon.Hydar.Cache.Phases {
   public abstract class CohortPhaseBase<TKey, TValue> : EpochPhaseBase<TKey, TValue> {
      public CohortState<TKey, TValue> CohortState => (CohortState<TKey, TValue>)EpochState;
      public EntryBlockTable<TKey, TValue> BlockTable => CohortState.BlockTable;
      public PartitionBlockIntervalConverter IntervalConverter => CohortState.IntervalConverter;

      public abstract CohortPartitioningState PartitioningState { get; }

      protected void SendCohortHeartBeat() {
         Messenger.CohortHeartBeat(Leader, LocalIdentifier);
      }
   }
}
