using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using ItzWarty;
using ItzWarty.Collections;
using System;
using System.Diagnostics;
using System.Linq;
using SCG = System.Collections.Generic;

namespace Dargon.Hydar {
   public class CacheRoot<TKey, TValue> {
      private readonly CourierEndpointImpl endpoint;
      private readonly MessageSenderImpl messageSender;
      private readonly CacheConfiguration cacheConfiguration;

      public CacheRoot(CourierEndpointImpl endpoint, MessageSenderImpl messageSender, CacheConfiguration cacheConfiguration) {
         this.endpoint = endpoint;
         this.messageSender = messageSender;
         this.cacheConfiguration = cacheConfiguration;
      }

      public class EpochState {
         public Guid Leader { get; set; }

         /// <summary>This array is ordered.</summary>
         public Guid[] Participants { get; set; }

         public Keyspace Keyspace { get; set; }
      }

      public class LeaderState : EpochState {
         public SubPhaseHost SubPhaseHost { get; set; }
         public IReadOnlySet<Guid> PendingOutsiders { get; set; }
      }

      public class CohortState : EpochState {
         public BlockTable BlockTable { get; set; }
      }

      public interface PhaseManager {
         void Transition(PhaseBase phase);
         void HandleTick();
         void Dispatch<TPayload>(IReceivedMessage<TPayload> message);
      }

      public class PhaseManagerImpl : PhaseManager {
         private readonly object synchronization = new object();
         protected PhaseBase currentPhase;

         public virtual string Name => "root";

         public virtual void Transition(PhaseBase phase) {
            lock (synchronization) {
               Console.WriteLine(Name + ": Transitioning " + (currentPhase?.ToString() ?? "[null]") + " => " + phase);

               currentPhase = phase;
               phase.HandleEntered();
            }
         }

         public void HandleTick() {
            lock (synchronization) {
               currentPhase.HandleTick();
            }  
         }

         public void Dispatch<TPayload>(IReceivedMessage<TPayload> message) {
            lock (synchronization) {
               currentPhase.Dispatch(message);
            }
         }
      }

      public class Block {
         public Block(int id) {
            Id = id;
         }

         public int Id { get; set; }
         public bool IsUpToDate { get; set; }

         public void BlahBlahEmpty() {
            IsUpToDate = true;
         }

         public void BlahBlahStale() {
            IsUpToDate = false;
         }
      }

      public class BlockTable {
         private readonly Keyspace keyspace;
         private readonly SCG.IReadOnlyList<Block> blocks;
         private IUniqueIdentificationSet haveBlocks = new UniqueIdentificationSet(false);

         public BlockTable(Keyspace keyspace, SCG.IReadOnlyList<Block> blocks) {
            this.keyspace = keyspace;
            this.blocks = blocks;
         }

         public void BlahBlahEmptyBlocks(IUniqueIdentificationSet set) {
            haveBlocks = haveBlocks.Merge(set);
            set.__Access(segments => {
               foreach (var segment in segments) {
                  for (var blockId = segment.low; blockId <= segment.high; blockId++) {
                     blocks[(int)blockId].BlahBlahEmpty();
                  }
               }
            });
         }

         public bool BlahBlahHave(uint blockId) {
            return blocks[(int)blockId].IsUpToDate;
         }

         public IUniqueIdentificationSet IntersectNeed(IUniqueIdentificationSet need) {
            return haveBlocks.Intersect(need);
         }
      }

      /// <summary>
      /// Supports emitting to a MessageSender and local MessageRouter.
      /// Useful for allowing subphase/host communication.
      /// </summary>
      public class SubphasedMessageSender : MessageSender {
         private readonly Guid localIdentifier;
         private readonly MessageSender outboundMessageSender;
         private readonly PhaseManager coPhaseManager;

         public SubphasedMessageSender(Guid localIdentifier, MessageSender outboundMessageSender, PhaseManager coPhaseManager) {
            this.localIdentifier = localIdentifier;
            this.outboundMessageSender = outboundMessageSender;
            this.coPhaseManager = coPhaseManager;
         }

         public void SendBroadcast<TPayload>(TPayload payload) {
            outboundMessageSender.SendBroadcast(payload);
            coPhaseManager.Dispatch(new ReceivedMessage<TPayload>(Guid.Empty, localIdentifier, localIdentifier, MessageFlags.Default, payload));
         }

         public void SendReliableUnicast<TMessage>(Guid recipientId, TMessage payload, MessagePriority priority) {
            outboundMessageSender.SendReliableUnicast(recipientId, payload, priority);
            if (recipientId == localIdentifier) {
               coPhaseManager.Dispatch(new ReceivedMessage<TMessage>(Guid.Empty, localIdentifier, localIdentifier, MessageFlags.AcknowledgementRequired, payload));
            }
         }

         public void SendUnreliableUnicast<TMessage>(Guid recipientId, TMessage message) {
            outboundMessageSender.SendUnreliableUnicast(recipientId, message);
            if (recipientId == localIdentifier) {
               coPhaseManager.Dispatch(new ReceivedMessage<TMessage>(Guid.Empty, localIdentifier, localIdentifier, MessageFlags.AcknowledgementRequired, message));
            }
         }
      }

      public class EpochMessageSender {
         private readonly MessageSender messageSender;

         public EpochMessageSender(MessageSender messageSender) {
            this.messageSender = messageSender;
         }

         public MessageSender __MessageSender => messageSender;

         public void Vote(Guid nominee) {
            messageSender.SendBroadcast(new ElectionVoteDto(nominee, null));
         }

         public void Vote(Guid nominee, IReadOnlySet<Guid> followers) {
            messageSender.SendBroadcast(new ElectionVoteDto(nominee, followers));
         }

         public void LeaderHeartBeat(Guid id, IReadOnlySet<Guid> participants) {
            messageSender.SendBroadcast(new LeaderHeartbeatDto(id, participants));
         }

         public void Need(PartitionBlockInterval[] neededBlocks) {
            messageSender.SendBroadcast(new CacheNeedDto(neededBlocks));
         }

         public void OutsiderAnnounce() {
            messageSender.SendBroadcast(new OutsiderAnnounceDto());
         }

         public void RepartitionSignal() {
            messageSender.SendBroadcast(new RepartitionSignalDto());
         }

         public void RepartitionCompletion() {
            messageSender.SendBroadcast(new RepartitionCompletionDto());
         }

         public EpochMessageSender WithMessageSender(MessageSender ms) => new EpochMessageSender(ms);
      }

      public class PhaseFactory {
         private const int kObliviousInitialTTC = 5;
         private const int kCandidateInitialTTV = 10;
         private const int kCandidateMaximumTTV = 10;
         private const int kFledgelingInitialTTM = 10;

         // Stuff for Factory
         private readonly ReceivedMessageFactory receivedMessageFactory;

         // Stuff for constructed phases' properties
         private readonly Guid localIdentifier;
         private readonly Keyspace keyspace;
         private readonly CacheRoot<TKey, TValue> cacheRoot;
         private readonly PhaseManager phaseManager;
         private readonly EpochMessageSender epochMessageSender;

         public PhaseFactory(ReceivedMessageFactory receivedMessageFactory, Guid localIdentifier, Keyspace keyspace, CacheRoot<TKey, TValue> cacheRoot, PhaseManager phaseManager, EpochMessageSender epochMessageSender) {
            this.receivedMessageFactory = receivedMessageFactory;
            this.localIdentifier = localIdentifier;
            this.keyspace = keyspace;
            this.cacheRoot = cacheRoot;
            this.phaseManager = phaseManager;
            this.epochMessageSender = epochMessageSender;
         }

         public PhaseBase Oblivious() {
            return Oblivious(kObliviousInitialTTC);
         }

         public PhaseBase Oblivious(int ticksToElectionCandidate) {
            return Initialize(new ObliviousPhase(ticksToElectionCandidate));
         }

         public PhaseBase ElectionCandidate() {
            return ElectionCandidate(kCandidateInitialTTV);
         }

         public PhaseBase ElectionCandidate(int nextTicksToVictory) {
            return ElectionCandidate(nextTicksToVictory, new HashSet<Guid>());
         }

         public PhaseBase ElectionCandidate(int nextTicksToVictory, ISet<Guid> followers) {
            return Initialize(new ElectionCandidatePhase(Math.Min(nextTicksToVictory, kCandidateMaximumTTV), followers));
         }

         public PhaseBase ElectionFollower(Guid nominee) {
            return ElectionFollower(nominee, false);
         }

         public PhaseBase ElectionFollower(Guid nominee, bool acknowledged) {
            return Initialize(new ElectionFollowerPhase(nominee, acknowledged));
         }

         public PhaseBase CoordinatorInitial(IReadOnlySet<Guid> participants) {
            var leaderState = new LeaderState {
               Leader = localIdentifier,
               Participants = participants.ToArray().With(Array.Sort),
               Keyspace = keyspace,
               PendingOutsiders = new HashSet<Guid>(),
               SubPhaseHost = new SubPhaseHost()
            };
            leaderState.SubPhaseHost.Transition(
               this.WithPhaseManager(leaderState.SubPhaseHost)
                   .WithMessenger(new EpochMessageSender(new SubphasedMessageSender(localIdentifier, epochMessageSender.__MessageSender, phaseManager)))
                   .CohortRepartitionInitial(localIdentifier, new HashSet<Guid>(participants))
            );
            var coordinatorInitialPhase = Initialize(new CoordinatorInitialPhase(), leaderState);
            var coordinatorMessenger = new EpochMessageSender(new SubphasedMessageSender(localIdentifier, epochMessageSender.__MessageSender, leaderState.SubPhaseHost));
            coordinatorInitialPhase.Messenger = coordinatorMessenger;
            coordinatorInitialPhase.PhaseFactory = WithMessenger(coordinatorMessenger);
            return coordinatorInitialPhase;
         }

         public PhaseBase CoordinatorRepartition(IReadOnlySet<Guid> remainingCohorts, LeaderState leaderState) {
            return Initialize(new CoordinatorRepartitionPhase(remainingCohorts), leaderState);
         }

         public PhaseBase CoordinatorPartitioned(LeaderState leaderState) {
            return Initialize(new CoordinatorPartitionedPhase(), leaderState);
         }

         public PhaseBase Outsider() {
            return Initialize(new OutsiderPhase());
         }

         public PhaseBase CohortRepartitionInitial(Guid leader, IReadOnlySet<Guid> participants) {
            var cohortState = new CohortState {
               Leader = leader,
               Participants = participants.ToArray().With(Array.Sort),
               Keyspace = keyspace,
               BlockTable = new BlockTable(keyspace, Util.Generate(keyspace.BlockCount, blockId => new Block(blockId)))
            };
            return Initialize(new CohortRepartitionInitialPhase(), cohortState);
         }

         public PhaseBase CohortRepartitioning(IUniqueIdentificationSet neededBlocks, CohortState cohortState) {
            return CohortRepartitioning(kFledgelingInitialTTM, neededBlocks, cohortState);
         }

         public PhaseBase CohortRepartitioning(int ticksToMaturity, IUniqueIdentificationSet neededBlocks, CohortState cohortState) {
            return Initialize(new CohortRepartitioningPhase(ticksToMaturity, neededBlocks), cohortState);
         }

         public PhaseBase CohortPartitioned(EpochState epochState) {
            return Initialize(new CohortPartitionedPhase(), epochState);
         }

         private PhaseBase Initialize(PhaseBase phase) {
            phase.LocalIdentifier = localIdentifier;
            phase.CacheRoot = cacheRoot;
            phase.PhaseManager = phaseManager;
            phase.PhaseFactory = this;
            phase.Router = new MessageRouterImpl(receivedMessageFactory);
            phase.Messenger = epochMessageSender;
            phase.Initialize();
            return phase;
         }

         private PhaseBase Initialize(EpochPhaseBase phase, EpochState epochState) {
            phase.EpochState = epochState;
            return Initialize((PhaseBase)phase);
         }

         public PhaseFactory WithPhaseManager(PhaseManager phaseManagerOverride) {
            return new PhaseFactory(receivedMessageFactory, localIdentifier, keyspace, cacheRoot, phaseManagerOverride, epochMessageSender);
         }

         public PhaseFactory WithMessenger(EpochMessageSender epochMessageSenderOverride) {
            return new PhaseFactory(receivedMessageFactory, localIdentifier, keyspace, cacheRoot, phaseManager, epochMessageSenderOverride);
         }
      }

      public abstract class PhaseBase {
         public Guid LocalIdentifier { get; set; }
         public CacheRoot<TKey, TValue> CacheRoot { get; set; } 
         public PhaseManager PhaseManager { get; set; }
         public PhaseFactory PhaseFactory { get; set; }
         public MessageRouter Router { get; set; }
         public EpochMessageSender Messenger { get; set; }

         public abstract void Initialize();
         public abstract void HandleEntered();
         public abstract void HandleTick();
         public virtual void Dispatch<TPayload>(IReceivedMessage<TPayload> message) => Router.RouteMessage(message);
      }

      public abstract class EpochPhaseBase : PhaseBase {
         public Guid Leader => EpochState.Leader;
         public SCG.IReadOnlyList<Guid> Participants => EpochState.Participants;
         public Keyspace Keyspace => EpochState.Keyspace;
         public EpochState EpochState { get; set; }

         public int Rank => GetLocalRank();

         private int localRankCache = -1;
         private int GetLocalRank() {
            if (localRankCache == -1) {
               localRankCache = Participants.OrderBy(x => x).ToList().IndexOf(LocalIdentifier);
            }
            return localRankCache;
         }
      }
      
      public abstract class CoordinatorPhaseBase : EpochPhaseBase {
         public LeaderState LeaderState => (LeaderState)EpochState;
         public SubPhaseHost SubPhaseHost => LeaderState.SubPhaseHost;
         public IReadOnlySet<Guid> PendingOutsiders => LeaderState.PendingOutsiders;

         protected void SendHeartBeat() {
            Messenger.LeaderHeartBeat(LocalIdentifier, new SortedSet<Guid>(Participants));
         }

         public override void Dispatch<TPayload>(IReceivedMessage<TPayload> message) {
            base.Dispatch(message);
            SubPhaseHost.Dispatch(message);
         }
      }

      public enum CohortPartitioningState {
         RepartitioningStarted,
         RepartitioningCompleting,
         Partitioned
      }

      public abstract class CohortPhaseBase : EpochPhaseBase {
         public CohortState CohortState => (CohortState)EpochState;
         public BlockTable BlockTable => CohortState.BlockTable;

         public abstract CohortPartitioningState PartitioningState { get; }
      }

      private class ObliviousPhase : PhaseBase {
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
               PhaseManager.Transition(PhaseFactory.CoordinatorInitial(followers));
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
               PhaseManager.Transition(PhaseFactory.CohortRepartitionInitial(message.Payload.Id, message.Payload.Participants));
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

      public class SubPhaseHost : PhaseManagerImpl {
         public override string Name => "leader_subphase";
         public CohortPhaseBase Phase => (CohortPhaseBase)currentPhase;

         public override void Transition(PhaseBase phase) {
            Trace.Assert(phase is CohortPhaseBase, "phase was not ICohortPhase");
            base.Transition(phase);
         }

         public bool IsInitialized => Phase != null;
         public CohortPartitioningState PartitioningState => Phase.PartitioningState;
      }

      public class CoordinatorInitialPhase : CoordinatorPhaseBase {
         public override void Initialize() { }

         public override void HandleEntered() {
            SendHeartBeat();
            PhaseManager.Transition(PhaseFactory.CoordinatorRepartition(new HashSet<Guid>(Participants), LeaderState));
         }

         public override void HandleTick() {
            // Can't happen.
         }

         public override string ToString() => $"[CoordinatorInitial]";
      }

      public class CoordinatorRepartitionPhase : CoordinatorPhaseBase {
         private readonly IReadOnlySet<Guid> remainingCohorts;

         public CoordinatorRepartitionPhase(IReadOnlySet<Guid> remainingCohorts) {
            this.remainingCohorts = remainingCohorts;
         }

         public override void Initialize() {
            Router.RegisterPayloadHandler<RepartitionCompletionDto>(HandleRepartitionCompletion);
         }

         public override void HandleEntered() {
            Messenger.RepartitionSignal();
            SendHeartBeat();
         }

         public override void HandleTick() {
            SubPhaseHost.HandleTick();
            Messenger.RepartitionSignal();
            SendHeartBeat();
         }

         private void HandleRepartitionCompletion(IReceivedMessage<RepartitionCompletionDto> message) {
            var nextRemainingCohorts = new HashSet<Guid>(remainingCohorts);
            nextRemainingCohorts.Remove(message.SenderId);
            if (nextRemainingCohorts.Count != 0) {
               PhaseManager.Transition(PhaseFactory.CoordinatorRepartition(nextRemainingCohorts, LeaderState));
            } else {
               PhaseManager.Transition(PhaseFactory.CoordinatorPartitioned(LeaderState));
            }
         }

         public override string ToString() => $"[CoordinatorRepartition ({remainingCohorts.Count} remaining)]";
      }

      public class CoordinatorPartitionedPhase : CoordinatorPhaseBase {
         public override void Initialize() { }

         public override void HandleEntered() { }

         public override void HandleTick() { }

         public override string ToString() => "[CoordinatorPartitioned]";
      }

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
            Console.WriteLine("Received Need: " + neededBlocks);
            var haveBlocks = BlockTable.IntersectNeed(neededBlocks);
            Console.WriteLine("Have: " + haveBlocks);
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
               PhaseManager.Transition(PhaseFactory.CohortPartitioned(EpochState));
            }
         }

         public override string ToString() => $"[CohortPartitioning Rank {Rank} TTM {ticksToMaturity}]";
      }

      public class CohortPartitionedPhase : CohortPhaseBase {
         public CohortPartitionedPhase() { }

         public override CohortPartitioningState PartitioningState => CohortPartitioningState.Partitioned;

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

      public class OutsiderPhase : PhaseBase {
         public override void Initialize() {
            
         }

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