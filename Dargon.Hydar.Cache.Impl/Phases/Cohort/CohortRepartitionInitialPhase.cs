using System;
using Dargon.Hydar.Cache.PortableObjects;

namespace Dargon.Hydar.Cache.Phases.Cohort {
   public class CohortRepartitionInitialPhase<TKey, TValue> : CohortPhaseBase<TKey, TValue> {
      public override CohortPartitioningState PartitioningState => CohortPartitioningState.RepartitioningStarted;

      public override void Initialize() {}

      public override void HandleEntered() {
         CacheOperationsManager.SuspendOperations();
         var neededBlockRanges = Keyspace.GetNodePartitionRanges(Rank, Participants.Length);
         var neededBlocks = IntervalConverter.ConvertToUidSet(neededBlockRanges);
         PhaseManager.Transition(PhaseFactory.CohortRepartitioning(neededBlocks, CohortState));
      }

      public override void HandleTick() {
         throw new InvalidOperationException();
      }

      public override string ToString() => $"[CohortRepartitionInitial]";
   }
}