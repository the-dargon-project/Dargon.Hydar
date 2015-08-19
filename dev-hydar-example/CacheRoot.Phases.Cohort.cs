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
            var neededBlockRanges = Keyspace.GetNodePartitionRanges(Rank, Participants.Count);
            var neededBlocks = IntervalConverter.ConvertToUidSet(neededBlockRanges);
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
            Router.RegisterPayloadHandler<CacheHaveDto>(HandleHave);
         }

         public override void HandleEntered() {
            SendNeed();
         }

         private void HandleNeed(IReceivedMessage<CacheNeedDto> message) {
            var neededBlocks = PartitionBlockInterval.ToUidSet(message.Payload.Blocks);
            var haveBlocks = BlockTable.IntersectNeed(neededBlocks);
            var haveIntervals = IntervalConverter.ConvertToPartitionBlockIntervals(haveBlocks);
            if (haveIntervals.Any()) {
               Console.WriteLine("Received Need: " + neededBlocks + " and have intersection " + haveBlocks + ".");
               Messenger.CacheHave(haveIntervals);
            } else {
               Console.WriteLine("Received Need: " + neededBlocks + " and have no matches.");
            }
         }

         private void SendNeed() {
            var intervals = IntervalConverter.ConvertToPartitionBlockIntervals(neededBlocks);
            Messenger.CacheNeed(intervals);
            Console.WriteLine("Sent Need " + intervals.Select(x => x.ToString()).Join(", "));
         }

         private void HandleHave(IReceivedMessage<CacheHaveDto> message) {
            Console.WriteLine("Received have: " + message.Payload.Blocks.Select(x => x.ToString()).Join(", "));
            var remoteHaveBlocks = IntervalConverter.ConvertToUidSet(message.Payload.Blocks);
            var availableBlocks = remoteHaveBlocks.Intersect(neededBlocks);
            Console.WriteLine("Need: " + neededBlocks + ", Available: " + availableBlocks + ", From: " + message.RemoteAddress);
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
         public override CohortPartitioningState PartitioningState => CohortPartitioningState.RepartitioningCompleting;

         public override void Initialize() {
            Router.RegisterPayloadHandler<LeaderRepartitionCompletingDto>(HandleLeaderRepartitionCompleting);
         }


         public override void HandleEntered() {
            Messenger.CohortRepartitionCompletion();
         }

         public override void HandleTick() {
            Messenger.CohortRepartitionCompletion();
         }

         private void HandleLeaderRepartitionCompleting(IReceivedMessage<LeaderRepartitionCompletingDto> x) {
            PhaseManager.Transition(PhaseFactory.CohortPartitioned(EpochState));
         }

         public override string ToString() => $"[CohortRepartitioningCompleted Rank {Rank} of {Participants.Count}]";
      }

      public class CohortPartitionedPhase : CohortPhaseBase {
         public override CohortPartitioningState PartitioningState => CohortPartitioningState.Partitioned;

         public override void Initialize() {
            Router.RegisterPayloadHandler<LeaderRepartitionSignalDto>(HandleLeaderRepartitionSignal);
         }

         public override void HandleEntered() {
            SendCohortHeartBeat();
         }

         public override void HandleTick() {
            SendCohortHeartBeat();
         }

         private void HandleLeaderRepartitionSignal(IReceivedMessage<LeaderRepartitionSignalDto> x) {
            if (!x.Payload.EpochId.Equals(EpochId)) {
               PhaseManager.Transition(PhaseFactory.CohortRepartitionInitial(x.Payload.EpochId, Leader, x.Payload.Participants, CohortState));
            }
         }

         public override string ToString() => $"[CohortPartitioned Rank {Rank} of {Participants.Count}]";
      }
   }
}
