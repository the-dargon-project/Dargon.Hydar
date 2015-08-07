using Dargon.Courier.Messaging;
using ItzWarty;
using ItzWarty.Collections;
using System;
using System.Linq;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
      public class CohortRepartitionInitialPhase : CohortPhaseBase {
         public override CohortPartitioningState PartitioningState => CohortPartitioningState.RepartitioningStarted;

         public override void Initialize() { }

         public override void HandleEntered() {
            var neededBlocks = new UniqueIdentificationSet(false);
            var neededBlockRanges = Keyspace.GetNodePartitionRanges(Rank, Participants.Count);
            neededBlockRanges.ForEach(x => {
               neededBlocks.GiveRange(x.StartBlockInclusive, x.EndBlockExclusive - 1);
            });
            PhaseManager.Transition(PhaseFactory.CohortRepartitioning(neededBlocks, CohortState));
         }

         public override void HandleTick() {
            throw new InvalidOperationException();
         }

         public override string ToString() => $"[CohortRepartitionInitial]";
      }

      public class CohortRepartitioningPhase : CohortPhaseBase {
         private readonly int ticksToMaturity;
         private readonly IUniqueIdentificationSet neededBlocks;

         public CohortRepartitioningPhase(int ticksToMaturity, IUniqueIdentificationSet neededBlocks) {
            this.ticksToMaturity = ticksToMaturity;
            this.neededBlocks = neededBlocks;
         }

         public override CohortPartitioningState PartitioningState => CohortPartitioningState.RepartitioningStarted;

         public override void Initialize() {
            Router.RegisterPayloadHandler<CacheNeedDto>(HandleNeed);
         }

         public override void HandleEntered() {
            SendNeed();
         }

         private void HandleNeed(IReceivedMessage<CacheNeedDto> message) {
            var neededBlocks = PartitionBlockInterval.ToUidSet(message.Payload.Blocks);
            var haveBlocks = BlockTable.IntersectNeed(neededBlocks);
            Console.WriteLine("Received Need: " + neededBlocks + " and have " + haveBlocks);
         }

         private void SendNeed() {
            PartitionBlockInterval[] intervals = null;
            neededBlocks.__Access(segments => {
               intervals = segments.Select(segment => new PartitionBlockInterval(segment.low, segment.high + 1)).ToArray();
            });
            Messenger.Need(intervals);
            Console.WriteLine("Sent Need " + intervals.Select(x => x.ToString()).Join(", "));
         }

         public override void HandleTick() {
            var nextTicksToMaturity = ticksToMaturity - 1;
            if (nextTicksToMaturity > 0) {
               PhaseManager.Transition(PhaseFactory.CohortRepartitioning(nextTicksToMaturity, neededBlocks, CohortState));
            } else {
               BlockTable.BlahBlahEmptyBlocks(neededBlocks);
               PhaseManager.Transition(PhaseFactory.CohortRepartitioningCompleted(EpochState));
            }
         }

         public override string ToString() => $"[CohortPartitioning Rank {Rank} TTM {ticksToMaturity}]";
      }

      public class CohortRepartitioningCompletedPhase : CohortPhaseBase {
         public CohortRepartitioningCompletedPhase() { }

         public override CohortPartitioningState PartitioningState => CohortPartitioningState.RepartitioningCompleting;

         public override void Initialize() {

         }

         public override void HandleEntered() {
            Messenger.RepartitionCompletion();
         }

         public override void HandleTick() {
            Messenger.RepartitionCompletion();
         }

         public override string ToString() => $"[CohortPartitioned Rank {Rank} of {Participants.Count}]";
      }
   }
}
