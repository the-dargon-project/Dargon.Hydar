using ItzWarty;
using ItzWarty.Collections;
using ItzWarty.Pooling;
using ItzWarty.Threading;
using System;
using System.Diagnostics;
using System.Threading;
using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using Dargon.Courier.Networking;
using Dargon.Courier.Peering;
using Dargon.Courier.PortableObjects;
using Dargon.PortableObjects;
using ItzWarty.IO;
using ItzWarty.Networking;

namespace Dargon.Hydar {
   public static class Program {
      public static void Main() {
         Console.Title = "PID " + Process.GetCurrentProcess().Id;

         // ItzWarty.Commons
         ICollectionFactory collectionFactory = new CollectionFactory();
         ObjectPoolFactory objectPoolFactory = new DefaultObjectPoolFactory(collectionFactory);

         // ItzWarty.Proxies
         IThreadingFactory threadingFactory = new ThreadingFactory();
         ISynchronizationFactory synchronizationFactory = new SynchronizationFactory();
         IThreadingProxy threadingProxy = new ThreadingProxy(threadingFactory, synchronizationFactory);
         GuidProxy guidProxy = new GuidProxyImpl();

         IDnsProxy dnsProxy = new DnsProxy();
         var tcpEndPointFactory = new TcpEndPointFactory(dnsProxy);
         IStreamFactory streamFactory = new StreamFactory();
         INetworkingInternalFactory networkingInternalFactory = new NetworkingInternalFactory(threadingProxy, streamFactory);
         ISocketFactory socketFactory = new SocketFactory(tcpEndPointFactory, networkingInternalFactory);
         INetworkingProxy networkingProxy = new NetworkingProxy(socketFactory, tcpEndPointFactory);

         // Dargon.PortableObjects
         var pofContext = new PofContext().With(x => {
            x.MergeContext(new DargonCourierImplPofContext());
            x.RegisterPortableObjectType(100001, typeof(ElectionVoteDto));
            x.RegisterPortableObjectType(100002, typeof(LeaderHeartbeatDto));
            x.RegisterPortableObjectType(100003, typeof(CacheNeedDto));
            x.RegisterPortableObjectType(100004, typeof(PartitionBlockInterval));
            x.RegisterPortableObjectType(100005, typeof(OutsiderAnnounceDto));
            x.RegisterPortableObjectType(100006, typeof(RepartitionSignalDto));
            x.RegisterPortableObjectType(100007, typeof(RepartitionCompletionDto));
         });
         var pofSerializer = new PofSerializer(pofContext);

         // Dargon.Courier for Networking
         var port = 50555;
         var identifier = Guid.NewGuid();
         var endpoint = new CourierEndpointImpl(pofSerializer, identifier, "node_?");
         var network = new UdpCourierNetwork(networkingProxy, new UdpCourierNetworkConfiguration(port));
         var networkContext = network.Join(endpoint);

         var networkBroadcaster = new NetworkBroadcasterImpl(endpoint, networkContext, pofSerializer);
         var messageContextPool = objectPoolFactory.CreatePool(() => new UnacknowledgedReliableMessageContext());
         var unacknowledgedReliableMessageContainer = new UnacknowledgedReliableMessageContainer(messageContextPool);
         var messageDtoPool = objectPoolFactory.CreatePool(() => new CourierMessageV1());
         var messageTransmitter = new MessageTransmitterImpl(guidProxy, pofSerializer, networkBroadcaster, unacknowledgedReliableMessageContainer, messageDtoPool);
         var messageSender = new MessageSenderImpl(guidProxy, unacknowledgedReliableMessageContainer, messageTransmitter);
         var acknowledgeDtoPool = objectPoolFactory.CreatePool(() => new CourierMessageAcknowledgeV1());
         var messageAcknowledger = new MessageAcknowledgerImpl(networkBroadcaster, unacknowledgedReliableMessageContainer, acknowledgeDtoPool);
         var periodicAnnouncer = new PeriodicAnnouncerImpl(threadingProxy, pofSerializer, endpoint, networkBroadcaster);
         periodicAnnouncer.Start();
         var periodicResender = new PeriodicResenderImpl(threadingProxy, unacknowledgedReliableMessageContainer, messageTransmitter);
         periodicResender.Start();

         ReceivedMessageFactory receivedMessageFactory = new ReceivedMessageFactoryImpl(pofSerializer);
         MessageRouter messageRouter = new MessageRouterImpl(receivedMessageFactory);
         var peerRegistry = new PeerRegistryImpl(pofSerializer);
         var networkReceiver = new NetworkReceiverImpl(endpoint, networkContext, pofSerializer, messageRouter, messageAcknowledger, peerRegistry);
         networkReceiver.Initialize();

         // Initialize Hydar
         var client = new ClusterClient(identifier);
         // var cache = client.StartCache<int, string>("test-cache");
         CacheConfiguration cacheConfiguration = new CacheConfiguration {
            Name = "test-cache"
         };
//         messageRouter.RegisterPayloadHandler<ElectionVoteDto>();
         var keyspace = new Keyspace(1024, 1);
         var cacheRoot = new CacheRoot<int, string>(endpoint, messageSender, cacheConfiguration);
         var cacheContext = new CacheContext<int, string>(cacheConfiguration);
         var messenger = new CacheRoot<int, string>.Messenger(messageSender);
         var phaseManager = new CacheRoot<int, string>.PhaseManagerImpl();
         var phaseFactory = new CacheRoot<int, string>.PhaseFactory(receivedMessageFactory, identifier, keyspace, cacheRoot, phaseManager, messenger);
         phaseManager.Transition(phaseFactory.Oblivious());

         messageRouter.RegisterPayloadHandler<ElectionVoteDto>(phaseManager.Dispatch);
         messageRouter.RegisterPayloadHandler<LeaderHeartbeatDto>(phaseManager.Dispatch);
         messageRouter.RegisterPayloadHandler<CacheNeedDto>(phaseManager.Dispatch);
         messageRouter.RegisterPayloadHandler<OutsiderAnnounceDto>(phaseManager.Dispatch);
         messageRouter.RegisterPayloadHandler<RepartitionSignalDto>(phaseManager.Dispatch);
         messageRouter.RegisterPayloadHandler<RepartitionCompletionDto>(phaseManager.Dispatch);

         new Thread(() => {
            while(true) {
               phaseManager.HandleTick();
               Thread.Sleep(100);
            }
         }).Start();

         var shutdown = new CountdownEvent(1);
         shutdown.Wait();
         //         const string kCacheNamespace = "test-cache";
         //         var cacheNamespace = client.JoinNamespace(kCacheNamespace, Role.Cohort);

         //         cacheNamespace.Propose(new { Key = 123, Action = Set, Params = 20 });
         //                  messageRouter.RegisterPayloadHandler<string>(m => {
         //                  });
      }
   }

   public class ElectionVoteDto : IPortableObject {
      public ElectionVoteDto() { }

      public ElectionVoteDto(Guid nominee, IReadOnlySet<Guid> followers) {
         this.Nominee = nominee;
         this.Followers = followers;
      }

      public Guid Nominee { get; set; }
      public IReadOnlySet<Guid> Followers { get; set; }

      public void Serialize(IPofWriter writer) {
         writer.WriteGuid(0, Nominee);
         writer.WriteCollection(1, Followers ?? new HashSet<Guid>());
      }

      public void Deserialize(IPofReader reader) {
         Nominee = reader.ReadGuid(0);
         Followers = reader.ReadCollection<Guid, HashSet<Guid>>(1);
      }
   }

   public class LeaderHeartbeatDto : IPortableObject {
      public LeaderHeartbeatDto() { }

      public LeaderHeartbeatDto(Guid id, IReadOnlySet<Guid> participants) {
         this.Id = id;
         this.Participants = participants;
      }

      public Guid Id { get; set; }
      public IReadOnlySet<Guid> Participants { get; set; }

      public void Serialize(IPofWriter writer) {
         writer.WriteGuid(0, Id);
         writer.WriteCollection(1, Participants);
      }

      public void Deserialize(IPofReader reader) {
         Id = reader.ReadGuid(0);
         Participants = reader.ReadCollection<Guid, HashSet<Guid>>(1);
      }
   }

   public class CacheNeedDto : IPortableObject {
      public CacheNeedDto() { }

      public CacheNeedDto(PartitionBlockInterval[] blocks) {
         Blocks = blocks;
      }

      public PartitionBlockInterval[] Blocks { get; set; }

      public void Serialize(IPofWriter writer) {
         writer.WriteCollection(0, Blocks);
      }

      public void Deserialize(IPofReader reader) {
         Blocks = reader.ReadArray<PartitionBlockInterval>(0);
      }
   }

   public class OutsiderAnnounceDto : IPortableObject {
      public void Serialize(IPofWriter writer) { }
      public void Deserialize(IPofReader reader) { }
   }

   public class RepartitionSignalDto : IPortableObject {
      public void Serialize(IPofWriter writer) { }
      public void Deserialize(IPofReader reader) { }
   }

   public class RepartitionCompletionDto : IPortableObject {
      public void Serialize(IPofWriter writer) { }
      public void Deserialize(IPofReader reader) { }
   }

   public enum Role {
      Cohort = 1
   }

   public enum CohortPartitioningState {
      RepartitioningStarted,
      RepartitioningCompleting,
      Partitioned
   }
}
