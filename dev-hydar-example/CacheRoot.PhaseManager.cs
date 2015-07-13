using Dargon.Courier.Messaging;
using System;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
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
   }
}
