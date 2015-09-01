using System;
using Dargon.Courier.Messaging;
using Dargon.Hydar.Cache.Phases.States;
using ItzWarty.Collections;

namespace Dargon.Hydar.Cache.Phases {
   public abstract class CoordinatorPhaseBase<TKey, TValue> : EpochPhaseBase<TKey, TValue> {
      public LeaderState<TKey, TValue> LeaderState => (LeaderState<TKey, TValue>)EpochState;
      public SubPhaseHost<TKey, TValue> SubPhaseHost => LeaderState.SubPhaseHost;
      public IReadOnlySet<Guid> PendingOutsiders => LeaderState.PendingOutsiders;

      protected void SendLeaderHeartBeat() {
         Messenger.LeaderHeartBeat(EpochId, Participants);
      }

      public override void Dispatch<TPayload>(IReceivedMessage<TPayload> message) {
         base.Dispatch(message);
         SubPhaseHost.Dispatch(message);
      }
   }
}