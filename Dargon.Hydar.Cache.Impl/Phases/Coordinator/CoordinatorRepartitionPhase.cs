using System;
using Dargon.Courier.Messaging;
using Dargon.Hydar.Cache.PortableObjects;
using ItzWarty.Collections;

namespace Dargon.Hydar.Cache.Phases.Coordinator {
   public class CoordinatorRepartitionPhase<TKey, TValue> : CoordinatorPhaseBase<TKey, TValue> {
      private readonly IReadOnlySet<Guid> remainingCohorts;

      public CoordinatorRepartitionPhase(IReadOnlySet<Guid> remainingCohorts) {
         this.remainingCohorts = remainingCohorts;
      }

      public override void Initialize() {
         Router.RegisterPayloadHandler<CohortRepartitionCompletionDto>(HandleRepartitionCompletion);
      }

      public override void HandleEntered() {
         Messenger.LeaderRepartitionSignal(EpochId, Participants);
      }

      public override void HandleTick() {
         SubPhaseHost.HandleTick();
         Messenger.LeaderRepartitionSignal(EpochId, Participants);
         SendLeaderHeartBeat();
      }

      private void HandleRepartitionCompletion(IReceivedMessage<CohortRepartitionCompletionDto> message) {
         var nextRemainingCohorts = new HashSet<Guid>(remainingCohorts);
         nextRemainingCohorts.Remove(message.SenderId);
         if (nextRemainingCohorts.Count != 0) {
            PhaseManager.Transition(PhaseFactory.CoordinatorRepartition(nextRemainingCohorts, LeaderState));
         } else {
            PhaseManager.Transition(PhaseFactory.CoordinatorPartitioningCompleting(new HashSet<Guid>(Participants), LeaderState));
         }
      }

      public override string ToString() => $"[CoordinatorRepartition ({remainingCohorts.Count} remaining)]";
   }
}