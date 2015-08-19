using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using Dargon.Hydar.Utilities;
using Dargon.Management;
using Dargon.Management.Server;
using Dargon.PortableObjects;
using Dargon.Services;
using ItzWarty;
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

      public class CacheMob {
         private readonly EntryBlockTable blockTable;

         public CacheMob(EntryBlockTable blockTable) {
            this.blockTable = blockTable;
         }

         [ManagedOperation]
         public string Hello() => "Hello, world!";

         [ManagedOperation]
         public TValue Get(TKey key) {
            try {
               return blockTable.Get(key);
            } catch (Exception e) {
               Console.WriteLine(e);
               return default(TValue);
            }
         }

         [ManagedOperation]
         public bool Put(TKey key, TValue value) {
            try {
               return blockTable.Put(key, value);
            } catch (Exception e) {
               Console.WriteLine(e);
               return false;
            }
         }
      }
   }

   public class CacheFactory {
      private const string kCacheMobNamePrefix = "@Hydar.";
      private readonly ManageableCourierEndpoint localEndpoint;
      private readonly MessageSender messageSender;
      private readonly MessageRouter messageRouter;
      private readonly ReceivedMessageFactory receivedMessageFactory;
      private readonly int servicePort;
      private readonly IServiceClient serviceClient;
      private readonly IServiceClientFactory serviceClientFactory;
      private readonly ILocalManagementServer localManagementServer;
      private readonly IPofContext pofContext;

      public CacheFactory(ManageableCourierEndpoint localEndpoint, IPofContext pofContext, MessageSender messageSender, MessageRouter messageRouter, ReceivedMessageFactory receivedMessageFactory, int servicePort, IServiceClient serviceClient, IServiceClientFactory serviceClientFactory, ILocalManagementServer localManagementServer) {
         this.localEndpoint = localEndpoint;
         this.pofContext = pofContext;
         this.messageSender = messageSender;
         this.messageRouter = messageRouter;
         this.receivedMessageFactory = receivedMessageFactory;
         this.servicePort = servicePort;
         this.serviceClient = serviceClient;
         this.serviceClientFactory = serviceClientFactory;
         this.localManagementServer = localManagementServer;
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
         var blocks = Util.Generate(keyspace.BlockCount, blockId => new CacheRoot<TKey, TValue>.Block(blockId));
         var blockTable = new CacheRoot<TKey, TValue>.EntryBlockTable(keyspace, blocks);
         var phaseFactory = new CacheRoot<TKey, TValue>.PhaseFactory(receivedMessageFactory, localEndpoint.Identifier, keyspace, cacheConfiguration, cacheRoot, phaseManager, messenger, remoteServiceContainer, blockTable);
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

         localManagementServer.RegisterContext(new ManagementContext(new CacheRoot<TKey, TValue>.CacheMob(blockTable), cacheGuid, kCacheMobNamePrefix + cacheName, pofContext));

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