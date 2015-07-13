using Dargon.Courier.Messaging;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
      public class ObliviousPhase : PhaseBase {
         private readonly int ticksToCandidate;

         public ObliviousPhase(int ticksToCandidate) {
            this.ticksToCandidate = ticksToCandidate;
         }

         public override void Initialize() {
            Router.RegisterPayloadHandler<ElectionVoteDto>(HandleElectionVote);
            Router.RegisterPayloadHandler<LeaderHeartbeatDto>(HandleLeaderHeartBeat);
         }

         public override void HandleEntered() {
            // do nothing
         }

         private void HandleElectionVote(IReceivedMessage<ElectionVoteDto> message) {
            if (LocalIdentifier.CompareTo(message.Payload.Nominee) > 0) {
               PhaseManager.Transition(PhaseFactory.ElectionCandidate());
            } else {
               PhaseManager.Transition(PhaseFactory.ElectionFollower(message.Payload.Nominee));
            }
         }

         private void HandleLeaderHeartBeat(IReceivedMessage<LeaderHeartbeatDto> message) {
            if (message.Payload.Participants.Contains(LocalIdentifier)) {
               PhaseManager.Transition(PhaseFactory.CohortRepartitionInitial(message.Payload.Id, message.Payload.Participants));
            } else {
               PhaseManager.Transition(PhaseFactory.Outsider());
            }
         }

         public override void HandleTick() {
            var nextTicks = ticksToCandidate - 1;
            if (nextTicks == 0) {
               PhaseManager.Transition(PhaseFactory.ElectionCandidate());
            } else {
               PhaseManager.Transition(PhaseFactory.Oblivious(nextTicks));
            }
         }

         public override string ToString() => $"[Oblivious {ticksToCandidate}]";
      }


   }
}
