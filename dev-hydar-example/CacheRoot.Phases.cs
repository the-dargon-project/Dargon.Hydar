using Dargon.Courier.Messaging;
using ItzWarty.Collections;
using System;
using System.Linq;
using SCG = System.Collections.Generic;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
      public abstract class PhaseBase {
         public Guid LocalIdentifier { get; set; }
         public CacheRoot<TKey, TValue> CacheRoot { get; set; } 
         public PhaseManager PhaseManager { get; set; }
         public PhaseFactory PhaseFactory { get; set; }
         public MessageRouter Router { get; set; }
         public Messenger Messenger { get; set; }

         public abstract void Initialize();
         public abstract void HandleEntered();
         public abstract void HandleTick();
         public virtual void Dispatch<TPayload>(IReceivedMessage<TPayload> message) => Router.RouteMessage(message);
      }

      public abstract class EpochPhaseBase : PhaseBase {
         public Guid EpochId => EpochState.EpochId;
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

         protected void SendLeaderHeartBeat() {
            Messenger.LeaderHeartBeat(EpochId, new SortedSet<Guid>(Participants));
         }

         public override void Dispatch<TPayload>(IReceivedMessage<TPayload> message) {
            base.Dispatch(message);
            SubPhaseHost.Dispatch(message);
         }
      }

      public abstract class CohortPhaseBase : EpochPhaseBase {
         public CohortState CohortState => (CohortState)EpochState;
         public BlockTable BlockTable => CohortState.BlockTable;

         public abstract CohortPartitioningState PartitioningState { get; }

         protected void SendCohortHeartBeat() {
            Messenger.CohortHeartBeat(Leader, LocalIdentifier);
         }
      }
   }
}
