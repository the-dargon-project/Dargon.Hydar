using System;
using Dargon.Courier.Messaging;
using Dargon.Hydar.Cache.PortableObjects;
using ItzWarty.Collections;

namespace Dargon.Hydar.Cache {
   public class CacheDispatcher {
      private readonly IConcurrentSet<ICacheRoot> caches = new ConcurrentSet<ICacheRoot>();
      private readonly IConcurrentDictionary<Guid, ICacheRoot> cachesById = new ConcurrentDictionary<Guid, ICacheRoot>();
      private readonly MessageRouter messageRouter;

      public CacheDispatcher(MessageRouter messageRouter) {
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
            ICacheRoot cacheRoot;
            if (cachesById.TryGetValue(cacheId, out cacheRoot)) {
               cacheRoot.Dispatch(message);
            }
         } else {
            throw new Exception(message.Payload.GetType().FullName);
         }
      }

      public void AddCache<TKey, TValue>(CacheRoot<TKey, TValue> cache) {
         caches.Add(cache);
         cachesById.Add(cache.Id, cache);
      }
   }

   public class CacheConfiguration {
      public string Name { get; set; }

      /// <summary>
      /// Gets the GUID of the cache, which is equivalent to the MD5 of the cache's name.
      /// </summary>
      public Guid Guid { get; set; }

      public int ServicePort { get; set; }
   }
}
