using Dargon.Hydar.Cache.Data;
using Dargon.Hydar.Cache.Phases.State;
using Dargon.Hydar.Cache.PortableObjects;

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
