using Dargon.Courier.Messaging;
using System;
using Dargon.Hydar.Cache.PortableObjects;

namespace Dargon.Hydar.Cache.Phases.Outsider {
   public class OutsiderPhase<TKey, TValue> : PhaseBase<TKey, TValue> {
      public override void Initialize() {
         Router.RegisterPayloadHandler<LeaderRepartitionSignalDto>(HandleLeaderRepartitionSignal);
      }

      public override void HandleEntered() {
         Messenger.OutsiderAnnounce();
      }

      public override void HandleTick() {
         Messenger.OutsiderAnnounce();
      }

      private void HandleLeaderRepartitionSignal(IReceivedMessage<LeaderRepartitionSignalDto> x) {
         if (Array.BinarySearch(x.Payload.ParticipantsOrdered, LocalIdentifier) >= 0) {
            PhaseManager.Transition(PhaseFactory.CohortRepartitionInitial(x.Payload.EpochId, x.SenderId, x.Payload.ParticipantsOrdered));
         }
      }

      public override string ToString() => $"[Outsider]";
   }
}
