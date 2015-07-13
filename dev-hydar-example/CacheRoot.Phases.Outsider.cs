using Dargon.Courier.Messaging;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
      public class OutsiderPhase : PhaseBase {
         public override void Initialize() { }

         public override void HandleEntered() {
            Messenger.OutsiderAnnounce();
         }

         public override void HandleTick() {
            Messenger.OutsiderAnnounce();
         }

         public override string ToString() => $"[Outsider]";
      }
   }
}
