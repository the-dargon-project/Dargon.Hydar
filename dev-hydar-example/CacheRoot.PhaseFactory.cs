using Dargon.Courier.Messaging;
using ItzWarty;
using ItzWarty.Collections;
using System;
using System.Linq;
using System.Net;
using SCG = System.Collections.Generic;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
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
         private readonly CacheConfiguration cacheConfiguration;
         private readonly CacheRoot<TKey, TValue> cacheRoot;
         private readonly PhaseManager phaseManager;
         private readonly Messenger messenger;
         private readonly RemoteServiceContainer remoteServiceContainer;
         private readonly EntryBlockTable blockTable;

         public PhaseFactory(ReceivedMessageFactory receivedMessageFactory, Guid localIdentifier, Keyspace keyspace, CacheConfiguration cacheConfiguration, CacheRoot<TKey, TValue> cacheRoot, PhaseManager phaseManager, Messenger messenger, RemoteServiceContainer remoteServiceContainer, EntryBlockTable blockTable) {
            this.receivedMessageFactory = receivedMessageFactory;
            this.localIdentifier = localIdentifier;
            this.keyspace = keyspace;
            this.cacheConfiguration = cacheConfiguration;
            this.cacheRoot = cacheRoot;
            this.phaseManager = phaseManager;
            this.messenger = messenger;
            this.remoteServiceContainer = remoteServiceContainer;
            this.blockTable = blockTable;
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

         public PhaseBase CoordinatorRepartitionInitial(IReadOnlySet<Guid> participants) {
            var epochId = Guid.NewGuid();
            var leaderState = new LeaderState {
               EpochId = epochId,
               Leader = localIdentifier,
               Participants = participants.ToArray().With(Array.Sort),
               Keyspace = keyspace,
               PendingOutsiders = new HashSet<Guid>(),
               SubPhaseHost = new SubPhaseHost()
            };
            leaderState.SubPhaseHost.Transition(
               this.WithPhaseManager(leaderState.SubPhaseHost)
                   .WithMessenger(new Messenger(new SubphasedMessageSender(localIdentifier, messenger.__MessageSender, phaseManager), cacheConfiguration))
                   .CohortRepartitionInitial(epochId, localIdentifier, new HashSet<Guid>(participants))
            );
            var coordinatorInitialPhase = Initialize(new CoordinatorInitialPhase(), leaderState);
            var coordinatorMessenger = new Messenger(new SubphasedMessageSender(localIdentifier, messenger.__MessageSender, leaderState.SubPhaseHost), cacheConfiguration);
            coordinatorInitialPhase.Messenger = coordinatorMessenger;
            coordinatorInitialPhase.PhaseFactory = WithMessenger(coordinatorMessenger);
            return coordinatorInitialPhase;
         }

         public PhaseBase CoordinatorRepartition(IReadOnlySet<Guid> remainingCohorts, LeaderState leaderState) {
            return Initialize(new CoordinatorRepartitionPhase(remainingCohorts), leaderState);
         }

         public PhaseBase CoordinatorPartitioningCompleting(IReadOnlySet<Guid> remainingCohorts, LeaderState leaderState) {
            return Initialize(new CoordinatorPartitioningCompletingPhase(remainingCohorts), leaderState);
         }

         public PhaseBase CoordinatorPartitioned(LeaderState leaderState) {
            return Initialize(new CoordinatorPartitionedPhase(), leaderState);
         }

         public PhaseBase Outsider() {
            return Initialize(new OutsiderPhase());
         }

         public PhaseBase CohortRepartitionInitial(Guid epochId, Guid leader, IReadOnlySet<Guid> participants) {
            var cohortState = new CohortState {
               Keyspace = keyspace,
               BlockTable = blockTable,
               IntervalConverter = new PartitionBlockIntervalConverterImpl()
            };
            return CohortRepartitionInitial(epochId, leader, participants, cohortState);
         }

         public PhaseBase CohortRepartitionInitial(Guid epochId, Guid leader, IReadOnlySet<Guid> participants, CohortState cohortState) {
            cohortState.EpochId = epochId;
            cohortState.Leader = leader;
            cohortState.Participants = participants.ToArray();
            return Initialize(new CohortRepartitionInitialPhase(), cohortState);
         }

         public PhaseBase CohortRepartitioning(IUniqueIdentificationSet neededBlocks, CohortState cohortState) {
            return CohortRepartitioning(kFledgelingInitialTTM, neededBlocks, cohortState);
         }

         public PhaseBase CohortRepartitioning(int ticksToMaturity, IUniqueIdentificationSet neededBlocks, CohortState cohortState) {
            return Initialize(new CohortRepartitioningPhase(ticksToMaturity, neededBlocks), cohortState);
         }

         public PhaseBase CohortRepartitioningCompleted(EpochState epochState) {
            return Initialize(new CohortRepartitioningCompletedPhase(), epochState);
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
            phase.Messenger = messenger;
            phase.RemoteServiceContainer = remoteServiceContainer;
            phase.Initialize();
            return phase;
         }

         private PhaseBase Initialize(EpochPhaseBase phase, EpochState epochState) {
            phase.EpochState = epochState;
            return Initialize((PhaseBase)phase);
         }

         public PhaseFactory WithPhaseManager(PhaseManager phaseManagerOverride) {
            return new PhaseFactory(receivedMessageFactory, localIdentifier, keyspace, cacheConfiguration, cacheRoot, phaseManagerOverride, messenger, remoteServiceContainer, blockTable);
         }

         public PhaseFactory WithMessenger(Messenger messengerOverride) {
            return new PhaseFactory(receivedMessageFactory, localIdentifier, keyspace, cacheConfiguration, cacheRoot, phaseManager, messengerOverride, remoteServiceContainer, blockTable);
         }
      }
   }
}
