using Dargon.Courier.Messaging;
using Dargon.Hydar.Cache.PortableObjects;
using ItzWarty.Collections;
using System;

namespace Dargon.Hydar.Cache {
   public class CacheDispatcherImpl : CacheDispatcher {
      private readonly IConcurrentSet<CacheRoot> caches = new ConcurrentSet<CacheRoot>();
      private readonly IConcurrentDictionary<Guid, CacheRoot> cachesById = new ConcurrentDictionary<Guid, CacheRoot>();
      private readonly MessageRouter messageRouter;

      public CacheDispatcherImpl(MessageRouter messageRouter) {
         this.messageRouter = messageRouter;
      }

      public void Initialize() {
         messageRouter.RegisterPayloadHandler<ElectionVoteDto>(Dispatch);
         messageRouter.RegisterPayloadHandler<LeaderHeartbeatDto>(Dispatch);
         messageRouter.RegisterPayloadHandler<CacheNeedDto>(Dispatch);
         messageRouter.RegisterPayloadHandler<CacheHaveDto>(Dispatch);
         messageRouter.RegisterPayloadHandler<OutsiderAnnounceDto>(Dispatch);
         messageRouter.RegisterPayloadHandler<LeaderRepartitionSignalDto>(Dispatch);
         messageRouter.RegisterPayloadHandler<CohortRepartitionCompletionDto>(Dispatch);
         messageRouter.RegisterPayloadHandler<LeaderRepartitionCompletingDto>(Dispatch);
         messageRouter.RegisterPayloadHandler<CohortHeartbeatDto>(Dispatch);
      }

      public void Dispatch<T>(IReceivedMessage<T> message) {
         var cacheMessage = message.Payload as HydarCacheMessageBase;
         if (cacheMessage != null) {
            var cacheId = cacheMessage.CacheId;
            CacheRoot cacheRoot;
            if (cachesById.TryGetValue(cacheId, out cacheRoot)) {
               cacheRoot.Dispatch(message);
            }
         } else {
            throw new Exception(message.Payload.GetType().FullName);
         }
      }

      public void RegisterCache(CacheRoot cache) {
         caches.Add(cache);
         cachesById.Add(cache.Id, cache);
      }
   }
}