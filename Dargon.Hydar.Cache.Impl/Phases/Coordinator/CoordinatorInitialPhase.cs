using System;
using ItzWarty.Collections;

namespace Dargon.Hydar.Cache.Phases.Coordinator {
   public class CoordinatorInitialPhase<TKey, TValue> : CoordinatorPhaseBase<TKey, TValue> {
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
}