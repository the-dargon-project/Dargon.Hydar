using System;
using Dargon.Courier.Messaging;
using Dargon.Hydar.Cache.PortableObjects;

namespace Dargon.Hydar.Cache.Phases.Oblivious {
   public class ObliviousPhase<TKey, TValue> : PhaseBase<TKey, TValue> {
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
         if (Array.BinarySearch(message.Payload.OrderedParticipants, LocalIdentifier) >= 0) {
            PhaseManager.Transition(PhaseFactory.CohortRepartitionInitial(message.Payload.EpochId, message.SenderId, message.Payload.OrderedParticipants));
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
