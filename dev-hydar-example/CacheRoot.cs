using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using Dargon.Hydar.Utilities;
using Dargon.Services;
using ItzWarty.Collections;
using ItzWarty.Networking;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
      private readonly ManageableCourierEndpoint endpoint;
      private readonly MessageSender messageSender;
      private readonly CacheConfiguration cacheConfiguration;
      private readonly RemoteServiceContainer remoteServiceContainer;

      public CacheRoot(ManageableCourierEndpoint endpoint, MessageSender messageSender, CacheConfiguration cacheConfiguration, RemoteServiceContainer remoteServiceContainer) {
         this.endpoint = endpoint;
         this.messageSender = messageSender;
         this.cacheConfiguration = cacheConfiguration;
         this.remoteServiceContainer = remoteServiceContainer;
      }
   }

   public class CacheFactory {
      private readonly ManageableCourierEndpoint localEndpoint;
      private readonly MessageSender messageSender;
      private readonly MessageRouter messageRouter;
      private readonly ReceivedMessageFactory receivedMessageFactory;
      private readonly int servicePort;
      private readonly IServiceClient serviceClient;
      private readonly IServiceClientFactory serviceClientFactory;

      public CacheFactory(ManageableCourierEndpoint localEndpoint, MessageSender messageSender, MessageRouter messageRouter, ReceivedMessageFactory receivedMessageFactory, int servicePort, IServiceClient serviceClient, IServiceClientFactory serviceClientFactory) {
         this.localEndpoint = localEndpoint;
         this.messageSender = messageSender;
         this.messageRouter = messageRouter;
         this.receivedMessageFactory = receivedMessageFactory;
         this.servicePort = servicePort;
         this.serviceClient = serviceClient;
         this.serviceClientFactory = serviceClientFactory;
      }

      public CacheRoot<TKey, TValue> Create<TKey, TValue>(string cacheName) {
         var guidHelper = new GuidHelperImpl();
         var cacheGuid = guidHelper.ComputeMd5(cacheName);

         CacheConfiguration cacheConfiguration = new CacheConfiguration {
            Name = cacheName,
            Guid = cacheGuid,
            ServicePort = servicePort
         };

         var serviceClientsByOrigin = new ConcurrentDictionary<IPEndPoint, IServiceClient>();
         var remoteServiceContainer = new CacheRoot<TKey, TValue>.RemoteServiceContainer(cacheConfiguration, serviceClientFactory, serviceClientsByOrigin);

         var keyspace = new Keyspace(1024, 1);
         var cacheRoot = new CacheRoot<TKey, TValue>(localEndpoint, messageSender, cacheConfiguration, remoteServiceContainer);
         var messenger = new CacheRoot<TKey, TValue>.Messenger(messageSender, cacheConfiguration);
         var phaseManager = new CacheRoot<TKey, TValue>.PhaseManagerImpl();
         var phaseFactory = new CacheRoot<TKey, TValue>.PhaseFactory(receivedMessageFactory, localEndpoint.Identifier, keyspace, cacheConfiguration, cacheRoot, phaseManager, messenger, remoteServiceContainer);
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