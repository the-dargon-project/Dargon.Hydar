using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using System;
using System.Linq;
using Dargon.Courier.PortableObjects;
using ImpromptuInterface;
using ItzWarty;
using ItzWarty.Collections;
using SCG = System.Collections.Generic;

namespace Dargon.Hydar {
   public class EpochManager<TKey, TValue> {
      private readonly CourierEndpointImpl endpoint;
      private readonly MessageSenderImpl messageSender;
      private readonly CacheConfiguration cacheConfiguration;

      public EpochManager(CourierEndpointImpl endpoint, MessageSenderImpl messageSender, CacheConfiguration cacheConfiguration) {
         this.endpoint = endpoint;
         this.messageSender = messageSender;
         this.cacheConfiguration = cacheConfiguration;
      }

      public interface PhaseManager {
         void Transition(PhaseBase phase);
         void HandleTick();
         void Dispatch<TPayload>(IReceivedMessage<TPayload> message);
      }

      public class PhaseManagerImpl : PhaseManager {
         private readonly object synchronization = new object();
         private PhaseBase currentPhase;

         public virtual string Name => "root";

         public void Transition(PhaseBase phase) {
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

      public class EpochMessageSender {
         private readonly MessageSender messageSender;

         public EpochMessageSender(MessageSender messageSender) {
            this.messageSender = messageSender;
         }

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
         private readonly EpochManager<TKey, TValue> epochManager;
         private readonly PhaseManager phaseManager;
         private readonly EpochMessageSender epochMessageSender;

         public PhaseFactory(ReceivedMessageFactory receivedMessageFactory, Guid localIdentifier, Keyspace keyspace, EpochManager<TKey, TValue> epochManager, PhaseManager phaseManager, EpochMessageSender epochMessageSender) {
            this.receivedMessageFactory = receivedMessageFactory;
            this.localIdentifier = localIdentifier;
            this.keyspace = keyspace;
            this.epochManager = epochManager;
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

         public PhaseBase Leader(IReadOnlySet<Guid> participants) {
            var epochState = new EpochState {
               Leader = localIdentifier,
               Participants = participants.ToArray().With(Array.Sort),
               Keyspace = keyspace
            };
            return Initialize(new LeaderPhase(), epochState);
         }

         public PhaseBase Outsider() {
            throw new NotImplementedException();
         }

         public PhaseBase Fledgeling(Guid leader, IReadOnlySet<Guid> participants) {
            var epochState = new EpochState {
               Leader = leader,
               Participants = participants.ToArray().With(Array.Sort),
               Keyspace = keyspace
            };
            return Initialize(new FledgelingPhase(kFledgelingInitialTTM), epochState);
         }

         public PhaseBase Fledgeling(int ticksToMaturity, EpochState epochState) {
            return Initialize(new FledgelingPhase(ticksToMaturity), epochState);
         }

         public PhaseBase Cohort(EpochState epochState) {
            return Initialize(new CohortPhase(), epochState);
         }

         private PhaseBase Initialize(PhaseBase phase) {
            phase.LocalIdentifier = localIdentifier;
            phase.EpochManager = epochManager;
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
            return new PhaseFactory(receivedMessageFactory, localIdentifier, keyspace, epochManager, phaseManagerOverride, epochMessageSender);
         }
      }

//      public class Router {
//         private readonly Dictionary<Type, Action<object>> handlersByType;
//
//         public Router() : this(new Dictionary<Type, Action<object>>()) {
//         }
//
//         public Router(Dictionary<Type, Action<object>> handlersByType) {
//            this.handlersByType = handlersByType;
//         }
//
//         public void Route(object o) {
//            handlersByType[o.GetType()](o);
//         }
//
//         public void RegisterHandler<T>(Action<T> handler) {
//            handlersByType.Add(typeof(T), x => handler((T)x));
//         }
//      }

      public abstract class PhaseBase {
         public Guid LocalIdentifier { get; set; }
         public EpochManager<TKey, TValue> EpochManager { get; set; } 
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
               PhaseManager.Transition(PhaseFactory.Fledgeling(message.Payload.Id, message.Payload.Participants));
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
         private readonly ItzWarty.Collections.ISet<Guid> followers;

         public ElectionCandidatePhase(int ticksToVictory, ItzWarty.Collections.ISet<Guid> followers) {
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
               PhaseManager.Transition(PhaseFactory.Leader(followers));
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
               PhaseManager.Transition(PhaseFactory.Fledgeling(message.Payload.Id, message.Payload.Participants));
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
      }

      public class LeaderPhase : EpochPhaseBase {
         private readonly SubPhaseHost subPhaseHost = new SubPhaseHost();

         public override void Initialize() {
            subPhaseHost.Transition(PhaseFactory.WithPhaseManager(subPhaseHost).Fledgeling(LocalIdentifier, new HashSet<Guid>(Participants)));
         }

         public override void HandleEntered() {
            SendHeartBeat();
         }

         public override void HandleTick() {
            SendHeartBeat();

            subPhaseHost.HandleTick();
         }

         public override string ToString() => $"[Leader of {Participants.Count}]";

         private void SendHeartBeat() {
            Messenger.LeaderHeartBeat(LocalIdentifier, new SortedSet<Guid>(Participants));
         }

         public override void Dispatch<TPayload>(IReceivedMessage<TPayload> message) {
            base.Dispatch(message);
            subPhaseHost.Dispatch(message);
         }
      }

      public class FledgelingPhase : EpochPhaseBase {
         private readonly int ticksToMaturity;

         public FledgelingPhase(int ticksToMaturity) {
            this.ticksToMaturity = ticksToMaturity;
         }

         public override void Initialize() {

         }

         public override void HandleEntered() {
            var neededBlocks = Keyspace.GetNodePartitionRanges(Rank, Participants.Count);
            Console.WriteLine("Fledgeling needs " + neededBlocks.Join(","));
            Messenger.Need(neededBlocks);
         }

         public override void HandleTick() {
            var nextTicksToMaturity = ticksToMaturity - 1;
            if (nextTicksToMaturity == 0) {
               PhaseManager.Transition(PhaseFactory.Cohort(EpochState));
            } else {
               PhaseManager.Transition(PhaseFactory.Fledgeling(nextTicksToMaturity, EpochState));
            }
         }

         public override string ToString() => $"[Fledgeling Rank {Rank} TTM {ticksToMaturity}]";
      }

      public class CohortPhase : EpochPhaseBase {
         public CohortPhase() { }

         public override void Initialize() {

         }

         public override void HandleEntered() {

         }

         public override void HandleTick() {

         }
      }
   }
}