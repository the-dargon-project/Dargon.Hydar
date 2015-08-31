using System;
using System.Linq;
using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using Dargon.Hydar.Cache.PortableObjects;
using ItzWarty;
using ItzWarty.Collections;

namespace Dargon.Hydar.Cache.Phases.Cohort {
   public class CohortRepartitioningPhase<TKey, TValue> : CohortPhaseBase<TKey, TValue> {
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
         if (Enumerable.Any<PartitionBlockInterval>(haveIntervals)) {
            Console.WriteLine("Received Need: " + neededBlocks + " and have intersection " + haveBlocks + ".");
            Messenger.CacheHave(haveIntervals);
         } else {
            Console.WriteLine("Received Need: " + neededBlocks + " and have no matches.");
         }
      }

      private void SendNeed() {
         var intervals = IntervalConverter.ConvertToPartitionBlockIntervals(neededBlocks);
         Messenger.CacheNeed(intervals);
         Console.WriteLine("Sent Need " + Enumerable.Select<PartitionBlockInterval, string>(intervals, x => x.ToString()).Join(", "));
      }

      private void HandleHave(IReceivedMessage<CacheHaveDto> message) {
         Console.WriteLine("Received have: " + message.Payload.Blocks.Select(x => x.ToString()).Join(", "));
         var remoteHaveBlocks = IntervalConverter.ConvertToUidSet(message.Payload.Blocks);
         var availableBlocks = remoteHaveBlocks.Intersect(neededBlocks);
         Console.WriteLine("Need: " + neededBlocks + ", Available: " + availableBlocks + ", From: " + message.RemoteAddress);

         if (availableBlocks.Any()) {
            var availableBlockIntervals = IntervalConverter.ConvertToPartitionBlockIntervals(availableBlocks);
            var remoteCacheService = RemoteServiceContainer.GetCacheService(message.RemoteAddress, message.Payload.ServicePort);
            var blockTransferResult = remoteCacheService.TransferBlocks(availableBlockIntervals);
            Console.WriteLine("Got " + blockTransferResult.Blocks.Count + " blocks from remote!");

            var nextNeededBlocks = neededBlocks.Except(availableBlocks);
            var nextTicksToMaturity = ticksToMaturity + 1;
            PhaseManager.Transition(PhaseFactory.CohortRepartitioning(nextTicksToMaturity, nextNeededBlocks, CohortState));
         }
      }

      public override void HandleTick() {
         var nextTicksToMaturity = ticksToMaturity - 1;
         if (nextTicksToMaturity > 0) {
            PhaseManager.Transition(PhaseFactory.CohortRepartitioning(nextTicksToMaturity, neededBlocks, CohortState));
         } else {
            bool permitProgression = true;
            foreach (var peerId in Participants) {
               if (peerId == LocalIdentifier) {
                  continue;
               }

               var remoteEndpoint = PeerRegistry.GetRemoteCourierEndpointOrNull(peerId);
               if (remoteEndpoint == null) {
                  permitProgression = false;
                  logger.Info("Not progressing further - have not received announce for " + peerId);
               } else {
                  var serviceDescriptor = CourierEndpointExtensions.GetPropertyOrDefault<HydarServiceDescriptor>(remoteEndpoint);
                  if (serviceDescriptor == null) {
                     permitProgression = false;
                     logger.Info("Not progressing further - have not received service descriptor for " + peerId);
                  }
               }
            }
            logger.Info(Enumerable.Count<ReadableCourierEndpoint>(PeerRegistry.EnumeratePeers()));
            if (permitProgression) {
               BlockTable.BlahBlahEmptyBlocks(neededBlocks);
               PhaseManager.Transition(PhaseFactory.CohortRepartitioningCompleted(EpochState));
            }
         }
      }

      public override string ToString() => $"[CohortPartitioning Rank {Rank} TTM {ticksToMaturity}]";
   }
}