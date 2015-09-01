using System;
using Dargon.Courier.Messaging;
using Dargon.Courier.Peering;
using Dargon.Hydar.Cache.Data.Operations;
using Dargon.Hydar.Cache.Messaging;
using Dargon.Hydar.Cache.Services;

namespace Dargon.Hydar.Cache.Phases {
   public abstract class PhaseBase<TKey, TValue> {
      public Guid LocalIdentifier { get; set; }
      public PhaseManager<TKey, TValue> PhaseManager { get; set; }
      public PhaseFactory<TKey, TValue> PhaseFactory { get; set; }
      public MessageRouter Router { get; set; }
      public Messenger<TKey, TValue> Messenger { get; set; }
      public RemoteServiceContainer<TKey, TValue> RemoteServiceContainer { get; set; }
      public CacheOperationsManager<TKey, TValue> CacheOperationsManager { get; set; }
      public ReadablePeerRegistry PeerRegistry { get; set; }

      public abstract void Initialize();
      public abstract void HandleEntered();
      public abstract void HandleTick();
      public virtual void Dispatch<TPayload>(IReceivedMessage<TPayload> message) => Router.RouteMessage(message);
   }
}