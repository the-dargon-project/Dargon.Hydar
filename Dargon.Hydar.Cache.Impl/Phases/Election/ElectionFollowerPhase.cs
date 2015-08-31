using System;
using Dargon.Courier.Messaging;
using Dargon.Hydar.Cache.PortableObjects;

namespace Dargon.Hydar.Cache.Phases.Election {
   public class ElectionFollowerPhase<TKey, TValue> : PhaseBase<TKey, TValue> {
      private readonly Guid nominee;
      private readonly bool acknowledged;

      public ElectionFollowerPhase(Guid nominee, bool acknowledged) {
         this.nominee = nominee;
         this.acknowledged = acknowledged;
      }

      public override void Initialize() {
         Router.RegisterPayloadHandler<ElectionVoteDto>(HandleElectionVote);
         Router.RegisterPayloadHandler<LeaderHeartbeatDto>(HandleLeaderHeartBeat);
      }

      private void HandleElectionVote(IReceivedMessage<ElectionVoteDto> message) {
         if (nominee.CompareTo(message.Payload.Nominee) < 0) {
            PhaseManager.Transition(PhaseFactory.ElectionFollower(message.Payload.Nominee));
         } else if (nominee.Equals(message.Payload.Nominee)) {
            if (message.SenderId.Equals(message.Payload.Nominee)) {
               if (!message.Payload.Followers.Contains(LocalIdentifier)) {
                  Messenger.Vote(nominee);
               } else if (!acknowledged) {
                  PhaseManager.Transition(PhaseFactory.ElectionFollower(nominee, true));
               }
            }
         } else {
            Messenger.Vote(nominee);
         }
      }

      private void HandleLeaderHeartBeat(IReceivedMessage<LeaderHeartbeatDto> message) {
         if (Array.BinarySearch(message.Payload.OrderedParticipants, LocalIdentifier) >= 0) {
            PhaseManager.Transition(PhaseFactory.CohortRepartitionInitial(message.Payload.EpochId, message.SenderId, message.Payload.OrderedParticipants));
         } else {
            PhaseManager.Transition(PhaseFactory.Outsider());
         }
      }

      public override void HandleEntered() {
         if (!acknowledged) {
            Messenger.Vote(nominee);
         }
      }

      public override void HandleTick() {
         if (!acknowledged) {
            Messenger.Vote(nominee);
         }
      }

      public override string ToString() => $"[Follower {nominee} {acknowledged}]";
   }
}