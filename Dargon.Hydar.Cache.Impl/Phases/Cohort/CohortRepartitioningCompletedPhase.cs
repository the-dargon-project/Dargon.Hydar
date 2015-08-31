using Dargon.Courier.Messaging;
using Dargon.Hydar.Cache.PortableObjects;

namespace Dargon.Hydar.Cache.Phases.Cohort {
   public class CohortRepartitioningCompletedPhase<TKey, TValue> : CohortPhaseBase<TKey, TValue> {
      public override CohortPartitioningState PartitioningState => CohortPartitioningState.RepartitioningCompleting;

      public override void Initialize() {
         Router.RegisterPayloadHandler<LeaderRepartitionCompletingDto>(HandleLeaderRepartitionCompleting);
      }


      public override void HandleEntered() {
         Messenger.CohortRepartitionCompletion();
      }

      public override void HandleTick() {
         Messenger.CohortRepartitionCompletion();
      }

      private void HandleLeaderRepartitionCompleting(IReceivedMessage<LeaderRepartitionCompletingDto> x) {
         PhaseManager.Transition(PhaseFactory.CohortPartitioned(EpochState));
      }

      public override string ToString() => $"[CohortRepartitioningCompleted Rank {Rank} of {Participants.Length}]";
   }
}