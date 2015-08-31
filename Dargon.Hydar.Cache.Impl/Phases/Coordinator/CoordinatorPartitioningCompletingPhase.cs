using System;
using System.Linq;
using Dargon.Courier.Messaging;
using Dargon.Hydar.Cache.PortableObjects;
using ItzWarty.Collections;

namespace Dargon.Hydar.Cache.Phases.Coordinator {
   public class CoordinatorPartitioningCompletingPhase<TKey, TValue> : CoordinatorPhaseBase<TKey, TValue> {
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
}