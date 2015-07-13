using Dargon.Courier.Messaging;
using System;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
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

   }
}
