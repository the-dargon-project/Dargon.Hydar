using System;
using Dargon.Courier.Messaging;
using Dargon.Hydar.Cache.PortableObjects;
using ItzWarty.Collections;

namespace Dargon.Hydar.Cache.Phases.Election {
   public class ElectionCandidatePhase<TKey, TValue> : PhaseBase<TKey, TValue> {
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
}
