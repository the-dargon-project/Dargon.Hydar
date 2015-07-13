using Dargon.Courier.Messaging;
using ItzWarty.Collections;
using System;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
      public class CoordinatorInitialPhase : CoordinatorPhaseBase {
         public override void Initialize() { }

         public override void HandleEntered() {
            SendHeartBeat();
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
            Router.RegisterPayloadHandler<RepartitionCompletionDto>(HandleRepartitionCompletion);
         }

         public override void HandleEntered() {
            Messenger.RepartitionSignal();
            SendHeartBeat();
         }

         public override void HandleTick() {
            SubPhaseHost.HandleTick();
            Messenger.RepartitionSignal();
            SendHeartBeat();
         }

         private void HandleRepartitionCompletion(IReceivedMessage<RepartitionCompletionDto> message) {
            var nextRemainingCohorts = new HashSet<Guid>(remainingCohorts);
            nextRemainingCohorts.Remove(message.SenderId);
            if (nextRemainingCohorts.Count != 0) {
               PhaseManager.Transition(PhaseFactory.CoordinatorRepartition(nextRemainingCohorts, LeaderState));
            } else {
               PhaseManager.Transition(PhaseFactory.CoordinatorPartitioned(LeaderState));
            }
         }

         public override string ToString() => $"[CoordinatorRepartition ({remainingCohorts.Count} remaining)]";
      }

      public class CoordinatorPartitionedPhase : CoordinatorPhaseBase {
         public override void Initialize() { }

         public override void HandleEntered() { }

         public override void HandleTick() { }

         public override string ToString() => "[CoordinatorPartitioned]";
      }
   }
}
