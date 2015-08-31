using System;
using Dargon.Courier.Messaging;

namespace Dargon.Hydar.Cache.Phases {
   public interface PhaseManager<TKey, TValue> {
      void Transition(PhaseBase<TKey, TValue> phase);
      void HandleTick();
      void Dispatch<TPayload>(IReceivedMessage<TPayload> message);
   }

   public class PhaseManagerImpl<TKey, TValue> : PhaseManager<TKey, TValue> {
      private readonly object synchronization = new object();
      protected PhaseBase<TKey, TValue> currentPhase;

      public virtual string Name => "root";

      public virtual void Transition(PhaseBase<TKey, TValue> phase) {
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
}
