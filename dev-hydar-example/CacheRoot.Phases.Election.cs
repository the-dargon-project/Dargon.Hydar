using Dargon.Courier.Messaging;
using ItzWarty.Collections;
using System;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
      public class ElectionCandidatePhase : PhaseBase {
         private readonly int ticksToVictory;
         private readonly ISet<Guid> followers;

         public ElectionCandidatePhase(int ticksToVictory, ISet<Guid> followers) {
            this.ticksToVictory = ticksToVictory;
            this.followers = followers;
         }

         public override void Initialize() {
            followers.Add(LocalIdentifier);

            Router.RegisterPayloadHandler<ElectionVoteDto>(HandleElectionVote);
         }

         private void HandleElectionVote(IReceivedMessage<ElectionVoteDto> message) {
            if (LocalIdentifier.CompareTo(message.Payload.Nominee) < 0) {
               PhaseManager.Transition(PhaseFactory.ElectionFollower(message.Payload.Nominee));
            } else {
               if (LocalIdentifier.Equals(message.Payload.Nominee)) {
                  followers.Add(message.SenderId);
               }

               // increment TTV as votes continue to roll in.
               var nextTicksToVictory = ticksToVictory + 1;
               PhaseManager.Transition(PhaseFactory.ElectionCandidate(nextTicksToVictory, followers));
            }
         }

         public override void HandleEntered() {
            Messenger.Vote(LocalIdentifier, followers);
         }

         public override void HandleTick() {
            var nextTicksToVictory = ticksToVictory - 1;
            if (nextTicksToVictory == 0) {
               PhaseManager.Transition(PhaseFactory.CoordinatorRepartitionInitial(followers));
            } else {
               PhaseManager.Transition(PhaseFactory.ElectionCandidate(nextTicksToVictory, followers));
            }
         }

         public override string ToString() => $"[Candidate {ticksToVictory}]";
      }

      public class ElectionFollowerPhase : PhaseBase {
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
            if (message.Payload.Participants.Contains(LocalIdentifier)) {
               PhaseManager.Transition(PhaseFactory.CohortRepartitionInitial(message.Payload.EpochId, message.SenderId, message.Payload.Participants));
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
}
