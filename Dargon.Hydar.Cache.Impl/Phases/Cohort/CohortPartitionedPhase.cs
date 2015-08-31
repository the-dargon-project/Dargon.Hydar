using Dargon.Courier.Messaging;
using Dargon.Hydar.Cache.Phases;
using Dargon.Hydar.Cache.PortableObjects;

namespace Dargon.Hydar.Cache.Phases.Cohort {
   public class CohortPartitionedPhase<TKey, TValue> : CohortPhaseBase<TKey, TValue> {
      public override CohortPartitioningState PartitioningState => CohortPartitioningState.Partitioned;

      public override void Initialize() {
         Router.RegisterPayloadHandler<LeaderRepartitionSignalDto>(HandleLeaderRepartitionSignal);
      }

      public override void HandleEntered() {
         CacheOperationsManager.ResumeOperations(Rank, Participants.Length, Participants);
         SendCohortHeartBeat();
      }

      public override void HandleTick() {
         SendCohortHeartBeat();
      }

      private void HandleLeaderRepartitionSignal(IReceivedMessage<LeaderRepartitionSignalDto> x) {
         if (!x.Payload.EpochId.Equals(EpochId)) {
            PhaseManager.Transition(PhaseFactory.CohortRepartitionInitial(x.Payload.EpochId, Leader, x.Payload.ParticipantsOrdered, CohortState));
         }
      }

      public override string ToString() => $"[CohortPartitioned Rank {Rank} of {Participants.Length}]";
   }
}
