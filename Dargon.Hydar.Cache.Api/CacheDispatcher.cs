using Dargon.Courier.Messaging;

namespace Dargon.Hydar.Cache {
   public interface CacheDispatcher {
      void Dispatch<T>(IReceivedMessage<T> message);
      void RegisterCache(CacheRoot cache);
   }
}
