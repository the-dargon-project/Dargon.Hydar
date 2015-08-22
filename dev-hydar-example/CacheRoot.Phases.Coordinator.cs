using Dargon.Courier.Messaging;
using ItzWarty.Collections;
using System;
using System.Linq;
using ItzWarty;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
      public class CoordinatorInitialPhase : CoordinatorPhaseBase {
         public override void Initialize() { }

         public override void HandleEntered() {
            SendLeaderHeartBeat();
            PhaseManager.Transition(PhaseFactory.CoordinatorRepartition(new HashSet<Guid>(Participants), LeaderState));
         }

         public override void HandleTick() {
            // Can't happen.
         }

         public override string ToString() => $"[CoordinatorInitial]";
      }

      public class CoordinatorRepartitionPhase : CoordinatorPhaseBase {
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

      public class CoordinatorPartitioningCompletingPhase : CoordinatorPhaseBase {
         private readonly IReadOnlySet<Guid> untransitionedCohortsRemaining;

         public CoordinatorPartitioningCompletingPhase(IReadOnlySet<Guid> untransitionedCohortsRemaining) {
            this.untransitionedCohortsRemaining = untransitionedCohortsRemaining;
         }

         public override void Initialize() {
            Router.RegisterPayloadHandler<CohortHeartbeatDto>(HandleCohortHeartbeat);
         }

         public override void HandleEntered() {
            Messenger.LeaderRepartitionCompleting();
            SendLeaderHeartBeat();
         }

         public override void HandleTick() {
            Messenger.LeaderRepartitionCompleting();
            SendLeaderHeartBeat();
         }

         private void HandleCohortHeartbeat(IReceivedMessage<CohortHeartbeatDto> message) {
            var nextCohortsRemaining = new HashSet<Guid>(untransitionedCohortsRemaining);
            nextCohortsRemaining.Remove(message.SenderId);
            if (nextCohortsRemaining.Any()) {
               PhaseManager.Transition(PhaseFactory.CoordinatorPartitioningCompleting(nextCohortsRemaining, LeaderState));
            } else {
               PhaseManager.Transition(PhaseFactory.CoordinatorPartitioned(LeaderState));
            }
         }

         public override string ToString() => $"[CoordinatorPartitioningCompletingPhase {untransitionedCohortsRemaining.Count}]";
      }

      public class CoordinatorPartitionedPhase : CoordinatorPhaseBase {
         public override void Initialize() {
            Router.RegisterPayloadHandler<OutsiderAnnounceDto>(HandleOutsiderAnnounce);
         }

         public override void HandleEntered() { }

         public override void HandleTick() {
            SendLeaderHeartBeat();
         }

         private void HandleOutsiderAnnounce(IReceivedMessage<OutsiderAnnounceDto> x) {
            SendLeaderHeartBeat();
            var nextParticipants = new HashSet<Guid>(Participants.Concat(x.SenderId).ToArray());
            PhaseManager.Transition(PhaseFactory.CoordinatorRepartitionInitial(nextParticipants));
         }

         public override string ToString() => "[CoordinatorPartitioned]";
      }
   }
}
