using Dargon.Courier.Messaging;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
      public class OutsiderPhase : PhaseBase {
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
            if (x.Payload.Participants.Contains(LocalIdentifier)) {
               PhaseManager.Transition(PhaseFactory.CohortRepartitionInitial(x.Payload.EpochId, x.SenderId, x.Payload.Participants));
            }
         }

         public override string ToString() => $"[Outsider]";
      }
   }
}
