using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Dargon.Courier;
using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using Dargon.Courier.Peering;
using Dargon.Hydar.Utilities;
using Dargon.Management;
using Dargon.Management.Server;
using Dargon.PortableObjects;
using Dargon.Ryu;
using Dargon.Services;
using ItzWarty;
using ItzWarty.Collections;
using ItzWarty.Networking;

namespace Dargon.Hydar {
   public interface ICacheRoot {
      void Dispatch<T>(IReceivedMessage<T> message);
   }

   public partial class CacheRoot<TKey, TValue> : ICacheRoot {
      private readonly string cacheName;
      private readonly Guid id;
      private readonly PhaseManager phaseManager;

      public CacheRoot(string cacheName, Guid id, PhaseManager phaseManager) {
         this.cacheName = cacheName;
         this.id = id;
         this.phaseManager = phaseManager;
      }

      public string Name => cacheName;
      public Guid Id => id;

      public void Dispatch<T>(IReceivedMessage<T> message) {
         phaseManager.Dispatch(message);
      }

      public class CacheMob {
         private readonly CacheOperationsManager cacheOperationsManager;

         public CacheMob(CacheOperationsManager cacheOperationsManager) {
            this.cacheOperationsManager = cacheOperationsManager;
         }

         [ManagedOperation]
         public string Hello() => "Hello, world!";

         [ManagedOperation]
         public TValue Get(TKey key) {
            try {
               var operation = new EntryOperationGet(key);
               return cacheOperationsManager.EnqueueAndAwaitResults(operation).Result;
            } catch (Exception e) {
               Console.WriteLine(e);
               return default(TValue);
            }
         }

         [ManagedOperation]
         public bool Put(TKey key, TValue value) {
            try {
               var operation = new EntryOperationPut(key, value);
               return cacheOperationsManager.EnqueueAndAwaitResults(operation).Result;
            } catch (Exception e) {
               Console.WriteLine(e);
               return false;
            }
         }
      }
   }

   public class CacheFactory {
      private const string kCacheMobNamePrefix = "@Hydar.";

      private readonly GuidHelper guidHelper;
      private readonly IServiceClientFactory serviceClientFactory;
      private readonly IServiceClient serviceClient;
      private readonly CourierClient courierClient;
      private readonly ILocalManagementServer localManagementServer;
      private readonly ReceivedMessageFactory receivedMessageFactory;
      private readonly IPofContext pofContext;
      private int servicePort;

      public CacheFactory(GuidHelper guidHelper, IServiceClientFactory serviceClientFactory, IServiceClient serviceClient, CourierClient courierClient, ILocalManagementServer localManagementServer, ReceivedMessageFactory receivedMessageFactory, IPofContext pofContext) {
         this.guidHelper = guidHelper;
         this.serviceClientFactory = serviceClientFactory;
         this.serviceClient = serviceClient;
         this.courierClient = courierClient;
         this.localManagementServer = localManagementServer;
         this.receivedMessageFactory = receivedMessageFactory;
         this.pofContext = pofContext;
      }

      public void SetServicePort(int newServicePort) {
         this.servicePort = newServicePort;
         courierClient.SetProperty(new HydarServiceDescriptor { ServicePort = servicePort });
      }

      public CacheRoot<TKey, TValue> Create<TKey, TValue>(string cacheName) {
         // Get Dependencies
         var localEndpoint = courierClient.LocalEndpoint;
         var messageSender = courierClient.MessageSender;
         var messageRouter = courierClient.MessageRouter;
         var peerRegistry = courierClient.PeerRegistry;

         var cacheGuid = guidHelper.ComputeMd5(cacheName);

         CacheConfiguration cacheConfiguration = new CacheConfiguration {
            Name = cacheName,
            Guid = cacheGuid,
            ServicePort = servicePort
         };

         var serviceClientsByOrigin = new ConcurrentDictionary<IPEndPoint, IServiceClient>();
         var remoteServiceContainer = new CacheRoot<TKey, TValue>.RemoteServiceContainer(cacheConfiguration, serviceClientFactory, peerRegistry, serviceClientsByOrigin);

         var keyspace = new Keyspace(1024, 1);
         var messenger = new CacheRoot<TKey, TValue>.Messenger(cacheGuid, messageSender, cacheConfiguration);
         var phaseManager = new CacheRoot<TKey, TValue>.PhaseManagerImpl();
         var cacheRoot = new CacheRoot<TKey, TValue>(cacheName, cacheGuid, phaseManager);
         var blocks = Util.Generate(keyspace.BlockCount, blockId => new CacheRoot<TKey, TValue>.Block(blockId));
         var blockTable = new CacheRoot<TKey, TValue>.EntryBlockTable(keyspace, blocks);
         var cacheOperationsManager = new CacheRoot<TKey, TValue>.CacheOperationsManager(keyspace, blockTable, remoteServiceContainer);
         var phaseFactory = new CacheRoot<TKey, TValue>.PhaseFactory(receivedMessageFactory, cacheGuid, localEndpoint.Identifier, keyspace, cacheConfiguration, cacheRoot, phaseManager, messenger, remoteServiceContainer, blockTable, cacheOperationsManager, peerRegistry);
         var cacheService = new CacheRoot<TKey, TValue>.CacheServiceImpl(cacheOperationsManager);
         serviceClient.RegisterService(cacheService, typeof(CacheRoot<TKey, TValue>.CacheService), cacheGuid);
         phaseManager.Transition(phaseFactory.Oblivious());

         localManagementServer.RegisterContext(new ManagementContext(new CacheRoot<TKey, TValue>.CacheMob(cacheOperationsManager), cacheGuid, kCacheMobNamePrefix + cacheName, pofContext));

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