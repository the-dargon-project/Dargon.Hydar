using System;
using System.Linq;
using Dargon.Courier.Messaging;
using Dargon.Courier.Peering;
using Dargon.Hydar.Cache.Data.Operations;
using Dargon.Hydar.Cache.Data.Partitioning;
using Dargon.Hydar.Cache.Messaging;
using Dargon.Hydar.Cache.Phases.Cohort;
using Dargon.Hydar.Cache.Phases.Coordinator;
using Dargon.Hydar.Cache.Phases.Election;
using Dargon.Hydar.Cache.Phases.Oblivious;
using Dargon.Hydar.Cache.Phases.Outsider;
using Dargon.Hydar.Cache.Phases.States;
using Dargon.Hydar.Cache.PortableObjects.Helpers;
using Dargon.Hydar.Cache.Services;
using ItzWarty;
using ItzWarty.Collections;

namespace Dargon.Hydar.Cache.Phases {
   public class PhaseFactory<TKey, TValue> {
      private const int kObliviousInitialTTC = 5;
      private const int kCandidateInitialTTV = 10;
      private const int kCandidateMaximumTTV = 10;
      private const int kFledgelingInitialTTM = 10;

      // Stuff for Factory
      private readonly ReceivedMessageFactory receivedMessageFactory;

      // Stuff for constructed phases' properties
      private readonly Guid cacheId;
      private readonly Guid localIdentifier;
      private readonly Keyspace keyspace;
      private readonly CacheConfiguration cacheConfiguration;
      private readonly CacheRoot<TKey, TValue> cacheRoot;
      private readonly PhaseManager<TKey, TValue> phaseManager;
      private readonly Messenger<TKey, TValue> messenger;
      private readonly RemoteServiceContainer<TKey, TValue> remoteServiceContainer;
      private readonly EntryBlockTable<TKey, TValue> blockTable;
      private readonly CacheOperationsManager<TKey, TValue> cacheOperationsManager;
      private readonly ReadablePeerRegistry peerRegistry;

      public PhaseFactory(ReceivedMessageFactory receivedMessageFactory, Guid cacheId, Guid localIdentifier, Keyspace keyspace, CacheConfiguration cacheConfiguration, CacheRoot<TKey, TValue> cacheRoot, PhaseManager<TKey, TValue> phaseManager, Messenger<TKey, TValue> messenger, RemoteServiceContainer<TKey, TValue> remoteServiceContainer, EntryBlockTable<TKey, TValue> blockTable, CacheOperationsManager<TKey, TValue> cacheOperationsManager, ReadablePeerRegistry peerRegistry) {
         this.cacheId = cacheId;
         this.receivedMessageFactory = receivedMessageFactory;
         this.localIdentifier = localIdentifier;
         this.keyspace = keyspace;
         this.cacheConfiguration = cacheConfiguration;
         this.cacheRoot = cacheRoot;
         this.phaseManager = phaseManager;
         this.messenger = messenger;
         this.remoteServiceContainer = remoteServiceContainer;
         this.blockTable = blockTable;
         this.cacheOperationsManager = cacheOperationsManager;
         this.peerRegistry = peerRegistry;
      }

      public PhaseBase<TKey, TValue> Oblivious() {
         return Oblivious(kObliviousInitialTTC);
      }

      public PhaseBase<TKey, TValue> Oblivious(int ticksToElectionCandidate) {
         return Initialize(new ObliviousPhase<TKey, TValue>(ticksToElectionCandidate));
      }

      public PhaseBase<TKey, TValue> ElectionCandidate() {
         return ElectionCandidate(kCandidateInitialTTV);
      }

      public PhaseBase<TKey, TValue> ElectionCandidate(int nextTicksToVictory) {
         return ElectionCandidate(nextTicksToVictory, new ItzWarty.Collections.HashSet<Guid>());
      }

      public PhaseBase<TKey, TValue> ElectionCandidate(int nextTicksToVictory, ItzWarty.Collections.ISet<Guid> followers) {
         return Initialize(new ElectionCandidatePhase<TKey, TValue>(Math.Min(nextTicksToVictory, kCandidateMaximumTTV), followers));
      }

      public PhaseBase<TKey, TValue> ElectionFollower(Guid nominee) {
         return ElectionFollower(nominee, false);
      }

      public PhaseBase<TKey, TValue> ElectionFollower(Guid nominee, bool acknowledged) {
         return Initialize(new ElectionFollowerPhase<TKey, TValue>(nominee, acknowledged));
      }

      public PhaseBase<TKey, TValue> CoordinatorRepartitionInitial(IReadOnlySet<Guid> participants) {
         var participantsOrdered = participants.ToArray().With(Array.Sort);
         var epochId = Guid.NewGuid();
         var leaderState = new LeaderState<TKey, TValue> {
            EpochId = epochId,
            Leader = localIdentifier,
            Participants = participantsOrdered,
            Keyspace = keyspace,
            PendingOutsiders = new HashSet<Guid>(),
            SubPhaseHost = new SubPhaseHost<TKey, TValue>()
         };
         leaderState.SubPhaseHost.Transition(
            this.WithPhaseManager(leaderState.SubPhaseHost)
                .WithMessenger(new Messenger<TKey, TValue>(cacheId, new SubphasedMessageSender<TKey, TValue>(localIdentifier, messenger.__MessageSender, phaseManager), cacheConfiguration))
                .CohortRepartitionInitial(epochId, localIdentifier, participantsOrdered)
         );
         var coordinatorInitialPhase = Initialize(new CoordinatorInitialPhase<TKey, TValue>(), leaderState);
         var coordinatorMessenger = new Messenger<TKey, TValue>(cacheId, new SubphasedMessageSender<TKey, TValue>(localIdentifier, messenger.__MessageSender, leaderState.SubPhaseHost), cacheConfiguration);
         coordinatorInitialPhase.Messenger = coordinatorMessenger;
         coordinatorInitialPhase.PhaseFactory = WithMessenger(coordinatorMessenger);
         return coordinatorInitialPhase;
      }

      public PhaseBase<TKey, TValue> CoordinatorRepartition(IReadOnlySet<Guid> remainingCohorts, LeaderState<TKey, TValue> leaderState) {
         return Initialize(new CoordinatorRepartitionPhase<TKey, TValue>(remainingCohorts), leaderState);
      }

      public PhaseBase<TKey, TValue> CoordinatorPartitioningCompleting(IReadOnlySet<Guid> remainingCohorts, LeaderState<TKey, TValue> leaderState) {
         return Initialize(new CoordinatorPartitioningCompletingPhase<TKey, TValue>(remainingCohorts), leaderState);
      }

      public PhaseBase<TKey, TValue> CoordinatorPartitioned(LeaderState<TKey, TValue> leaderState) {
         return Initialize(new CoordinatorPartitionedPhase<TKey, TValue>(), leaderState);
      }

      public PhaseBase<TKey, TValue> Outsider() {
         return Initialize(new OutsiderPhase<TKey, TValue>());
      }

      public PhaseBase<TKey, TValue> CohortRepartitionInitial(Guid epochId, Guid leader, Guid[] participants) {
         var cohortState = new CohortState<TKey, TValue> {
            Keyspace = keyspace,
            BlockTable = blockTable,
            IntervalConverter = new PartitionBlockIntervalConverterImpl()
         };
         return CohortRepartitionInitial(epochId, leader, participants, cohortState);
      }

      public PhaseBase<TKey, TValue> CohortRepartitionInitial(Guid epochId, Guid leader, Guid[] participants, CohortState<TKey, TValue> cohortState) {
         cohortState.EpochId = epochId;
         cohortState.Leader = leader;
         cohortState.Participants = participants;
         return Initialize(new CohortRepartitionInitialPhase<TKey, TValue>(), cohortState);
      }

      public PhaseBase<TKey, TValue> CohortRepartitioning(IUniqueIdentificationSet neededBlocks, CohortState<TKey, TValue> cohortState) {
         return CohortRepartitioning(kFledgelingInitialTTM, neededBlocks, cohortState);
      }

      public PhaseBase<TKey, TValue> CohortRepartitioning(int ticksToMaturity, IUniqueIdentificationSet neededBlocks, CohortState<TKey, TValue> cohortState) {
         return Initialize(new CohortRepartitioningPhase<TKey, TValue>(ticksToMaturity, neededBlocks), cohortState);
      }

      public PhaseBase<TKey, TValue> CohortRepartitioningCompleted(EpochState<TKey, TValue> epochState) {
         return Initialize(new CohortRepartitioningCompletedPhase<TKey, TValue>(), epochState);
      }

      public PhaseBase<TKey, TValue> CohortPartitioned(EpochState<TKey, TValue> epochState) {
         return Initialize(new CohortPartitionedPhase<TKey, TValue>(), epochState);
      }

      private PhaseBase<TKey, TValue> Initialize(PhaseBase<TKey, TValue> phase) {
         phase.LocalIdentifier = localIdentifier;
         phase.CacheRoot = cacheRoot;
         phase.PhaseManager = phaseManager;
         phase.PhaseFactory = this;
         phase.Router = new MessageRouterImpl();
         phase.Messenger = messenger;
         phase.RemoteServiceContainer = remoteServiceContainer;
         phase.CacheOperationsManager = cacheOperationsManager;
         phase.PeerRegistry = peerRegistry;
         phase.Initialize();
         return phase;
      }

      private PhaseBase<TKey, TValue> Initialize(EpochPhaseBase<TKey, TValue> phase, EpochState<TKey, TValue> epochState) {
         phase.EpochState = epochState;
         return Initialize((PhaseBase<TKey, TValue>)phase);
      }

      public PhaseFactory<TKey, TValue> WithPhaseManager(PhaseManager<TKey, TValue> phaseManagerOverride) {
         return new PhaseFactory<TKey, TValue>(receivedMessageFactory, cacheId, localIdentifier, keyspace, cacheConfiguration, cacheRoot, phaseManagerOverride, messenger, remoteServiceContainer, blockTable, cacheOperationsManager, peerRegistry);
      }

      public PhaseFactory<TKey, TValue> WithMessenger(Messenger<TKey, TValue> messengerOverride) {
         return new PhaseFactory<TKey, TValue>(receivedMessageFactory, cacheId, localIdentifier, keyspace, cacheConfiguration, cacheRoot, phaseManager, messengerOverride, remoteServiceContainer, blockTable, cacheOperationsManager, peerRegistry);
      }
   }
}
