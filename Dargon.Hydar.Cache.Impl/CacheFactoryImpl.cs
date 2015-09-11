using System;
using Dargon.Courier;
using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using Dargon.Hydar.Cache.Data.Operations;
using Dargon.Hydar.Cache.Data.Partitioning;
using Dargon.Hydar.Cache.Management;
using Dargon.Hydar.Cache.Messaging;
using Dargon.Hydar.Cache.Phases;
using Dargon.Hydar.Cache.PortableObjects;
using Dargon.Hydar.Cache.Services;
using Dargon.Hydar.Common.Utilities;
using Dargon.Management.Server;
using Dargon.PortableObjects;
using Dargon.Services;
using ItzWarty;
using ItzWarty.Collections;
using System.Net;
using System.Threading;
using Dargon.Hydar.Cache.Data.Storage;
using Dargon.Management;

namespace Dargon.Hydar.Cache {
   public class CacheFactoryImpl : CacheFactory {
      private const string kCacheMobNamePrefix = "@Hydar.";

      private readonly GuidHelper guidHelper;
      private readonly ServiceClientFactory serviceClientFactory;
      private readonly ServiceClient serviceClient;
      private readonly CourierClient courierClient;
      private readonly ILocalManagementServer localManagementServer;
      private readonly ReceivedMessageFactory receivedMessageFactory;
      private readonly IPofContext pofContext;

      public CacheFactoryImpl(GuidHelper guidHelper, ServiceClientFactory serviceClientFactory, ServiceClient serviceClient, CourierClient courierClient, ILocalManagementServer localManagementServer, ReceivedMessageFactory receivedMessageFactory, IPofContext pofContext) {
         this.guidHelper = guidHelper;
         this.serviceClientFactory = serviceClientFactory;
         this.serviceClient = serviceClient;
         this.courierClient = courierClient;
         this.localManagementServer = localManagementServer;
         this.receivedMessageFactory = receivedMessageFactory;
         this.pofContext = pofContext;
      }

      public CacheRoot<TKey, TValue> Create<TKey, TValue>(CacheConfiguration<TKey, TValue> cacheConfiguration) {
         if (cacheConfiguration.Name == null) {
            throw new ArgumentNullException(nameof(cacheConfiguration.Name));
         }
         if (cacheConfiguration.ServicePort <= 0) {
            throw new ArgumentException(nameof(cacheConfiguration.ServicePort));
         }

         // Get Dependencies
         var localEndpoint = courierClient.LocalEndpoint;
         var messageSender = courierClient.MessageSender;
         var messageRouter = courierClient.MessageRouter;
         var peerRegistry = courierClient.PeerRegistry;

         var cacheName = cacheConfiguration.Name;
         if (cacheConfiguration.Guid.Equals(Guid.Empty)) {
            cacheConfiguration.Guid = guidHelper.ComputeMd5(cacheName);
         }

         var cacheGuid = cacheConfiguration.Guid;

         if (cacheConfiguration.Storage == null) {
            cacheConfiguration.Storage = new NullCacheStore<TKey, TValue>();
         }
         var cacheStore = cacheConfiguration.Storage;

         var serviceClientsByOrigin = new ConcurrentDictionary<IPEndPoint, ServiceClient>();
         var remoteServiceContainer = new RemoteServiceContainer<TKey, TValue>(cacheConfiguration, serviceClientFactory, peerRegistry, serviceClientsByOrigin);

         var keyspace = new Keyspace(1024, 1);
         var messenger = new Messenger<TKey, TValue>(cacheGuid, messageSender, cacheConfiguration);
         var phaseManager = new PhaseManagerImpl<TKey, TValue>();
         var cacheStrategy = new WriteThroughCacheStorageStrategyImpl<TKey, TValue>(cacheStore);
         var blocks = Util.Generate(keyspace.BlockCount, blockId => new Block<TKey, TValue>(blockId, cacheStrategy));
         var blockTable = new EntryBlockTable<TKey, TValue>(keyspace, blocks);
         var cacheOperationsManager = new CacheOperationsManager<TKey, TValue>(keyspace, blockTable, remoteServiceContainer);
         var phaseFactory = new PhaseFactory<TKey, TValue>(receivedMessageFactory, cacheGuid, localEndpoint.Identifier, keyspace, cacheConfiguration, phaseManager, messenger, remoteServiceContainer, blockTable, cacheOperationsManager, peerRegistry);
         var cacheService = new CacheServiceImpl<TKey, TValue>(cacheOperationsManager);
         serviceClient.RegisterService(cacheService, typeof(CacheService<TKey, TValue>), cacheGuid);
         phaseManager.Transition(phaseFactory.Oblivious());

//         localManagementServer.RegisterInstance(new TrashMob());
         localManagementServer.RegisterContext(new ManagementContext(new CacheMob<TKey, TValue>(cacheOperationsManager), cacheGuid, kCacheMobNamePrefix + cacheName, pofContext));

         new Thread(() => {
            while (true) {
               phaseManager.HandleTick();
               Thread.Sleep(100);
            }
         }).Start();
         
         return new CacheRootImpl<TKey, TValue>(cacheName, cacheGuid, phaseManager, cacheService);
      }
   }

   public class TrashMob {
      [ManagedProperty]
      public string HelloWorld { get; set; }
   }
}