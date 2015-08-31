using System;
using System.Linq;
using Dargon.Courier.Messaging;
using Dargon.Hydar.Cache.PortableObjects;
using ItzWarty;
using ItzWarty.Collections;

namespace Dargon.Hydar.Cache.Phases.Coordinator {
   public class CoordinatorPartitionedPhase<TKey, TValue> : CoordinatorPhaseBase<TKey, TValue> {
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
