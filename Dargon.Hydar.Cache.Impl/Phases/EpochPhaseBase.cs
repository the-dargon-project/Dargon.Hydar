using System;
using Dargon.Hydar.Cache.Phases.State;
using NLog;

namespace Dargon.Hydar.Cache.Phases {
   public abstract class EpochPhaseBase<TKey, TValue> : PhaseBase<TKey, TValue> {
      protected readonly Logger logger;

      protected EpochPhaseBase() {
         logger = LogManager.GetLogger(GetType().Name);
      }

      public Guid EpochId => EpochState.EpochId;
      public Guid Leader => EpochState.Leader;
      // Ordered by rank
      public Guid[] Participants => EpochState.Participants;
      public Keyspace Keyspace => EpochState.Keyspace;
      public EpochState<TKey, TValue> EpochState { get; set; }

      public int Rank => GetLocalRank();

      private int localRankCache = -1;
      private int GetLocalRank() {
         if (localRankCache == -1) {
            localRankCache = Array.BinarySearch(Participants, LocalIdentifier);
         }
         return localRankCache;
      }
   }
}