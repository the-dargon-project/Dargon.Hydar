using System;
using System.Threading;
using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using Dargon.Hydar.Utilities;
using Dargon.Services;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
      private readonly ManageableCourierEndpoint endpoint;
      private readonly MessageSender messageSender;
      private readonly CacheConfiguration cacheConfiguration;

      public CacheRoot(ManageableCourierEndpoint endpoint, MessageSender messageSender, CacheConfiguration cacheConfiguration) {
         this.endpoint = endpoint;
         this.messageSender = messageSender;
         this.cacheConfiguration = cacheConfiguration;
      }
   }

   public class CacheFactory {
      private readonly ManageableCourierEndpoint localEndpoint;
      private readonly MessageSender messageSender;
      private readonly MessageRouter messageRouter;
      private readonly ReceivedMessageFactory receivedMessageFactory;
      private readonly IServiceClient serviceClient;

      public CacheFactory(ManageableCourierEndpoint localEndpoint, MessageSender messageSender, MessageRouter messageRouter, ReceivedMessageFactory receivedMessageFactory, IServiceClient serviceClient) {
         this.localEndpoint = localEndpoint;
         this.messageSender = messageSender;
         this.messageRouter = messageRouter;
         this.receivedMessageFactory = receivedMessageFactory;
         this.serviceClient = serviceClient;
      }

      public CacheRoot<TKey, TValue> Create<TKey, TValue>(string cacheName) {
         var guidHelper = new GuidHelperImpl();
         var cacheGuid = guidHelper.ComputeMd5(cacheName);

         CacheConfiguration cacheConfiguration = new CacheConfiguration {
            Name = cacheName,
            Guid = cacheGuid
         };

         var keyspace = new Keyspace(1024, 1);
         var cacheRoot = new CacheRoot<TKey, TValue>(localEndpoint, messageSender, cacheConfiguration);
         var messenger = new CacheRoot<TKey, TValue>.Messenger(messageSender);
         var phaseManager = new CacheRoot<TKey, TValue>.PhaseManagerImpl();
         var phaseFactory = new CacheRoot<TKey, TValue>.PhaseFactory(receivedMessageFactory, localEndpoint.Identifier, keyspace, cacheRoot, phaseManager, messenger);
         var cacheService = new CacheRoot<TKey, TValue>.CacheServiceImpl();
         serviceClient.RegisterService(cacheService, typeof(CacheRoot<TKey, TValue>.CacheService), cacheGuid);
         phaseManager.Transition(phaseFactory.Oblivious());

         messageRouter.RegisterPayloadHandler<ElectionVoteDto>(phaseManager.Dispatch);
         messageRouter.RegisterPayloadHandler<LeaderHeartbeatDto>(phaseManager.Dispatch);
         messageRouter.RegisterPayloadHandler<CacheNeedDto>(phaseManager.Dispatch);
         messageRouter.RegisterPayloadHandler<CacheHaveDto>(phaseManager.Dispatch);
         messageRouter.RegisterPayloadHandler<OutsiderAnnounceDto>(phaseManager.Dispatch);
         messageRouter.RegisterPayloadHandler<LeaderRepartitionSignalDto>(phaseManager.Dispatch);
         messageRouter.RegisterPayloadHandler<CohortRepartitionCompletionDto>(phaseManager.Dispatch);
         messageRouter.RegisterPayloadHandler<LeaderRepartitionCompletingDto>(phaseManager.Dispatch);
         messageRouter.RegisterPayloadHandler<CohortHeartbeatDto>(phaseManager.Dispatch);

         new Thread(() => {
            while (true) {
               phaseManager.HandleTick();
               Thread.Sleep(100);
            }
         }).Start();

         return cacheRoot;
      }
   }
}