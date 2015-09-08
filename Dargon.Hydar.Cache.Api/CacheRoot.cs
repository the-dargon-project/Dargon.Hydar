using Dargon.Courier.Messaging;
using System;

namespace Dargon.Hydar.Cache {
   public interface CacheRoot {
      string Name { get; }
      Guid Id { get; }

      void Dispatch<T>(IReceivedMessage<T> message);
   }

   public interface CacheRoot<TKey, TValue> : Cache<TKey, TValue>, CacheRoot { }
}