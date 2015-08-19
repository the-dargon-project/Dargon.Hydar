using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using Dargon.Courier.Networking;
using Dargon.Courier.Peering;
using Dargon.Courier.PortableObjects;
using Dargon.PortableObjects;
using ItzWarty;
using ItzWarty.Collections;
using ItzWarty.IO;
using ItzWarty.Networking;
using ItzWarty.Pooling;
using ItzWarty.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading;
using Castle.DynamicProxy;
using CommandLine;
using Dargon.Hydar.Utilities;
using Dargon.Nest.Egg;
using Dargon.PortableObjects.Streams;
using Dargon.Services;

namespace Dargon.Hydar {
   public static class Program {
      public class Options {
         [Option('s', "ServicePort", DefaultValue = 32000, HelpText = "Dedicated port for hydar cache service dsp.")]
         public int ServicePort { get; set; }

         [Option('m', "ManagementPort", DefaultValue = 32002, HelpText = "Dedicated port for hydar cache service dmi.")]
         public int ManagementPort { get; set; }
      }

      public static void Main(string[] args) {
         Console.Title = "PID " + Process.GetCurrentProcess().Id;
         var options = new Options();
         if (Parser.Default.ParseArgumentsStrict(args, options)) {
            new HydarEgg().Start(options.ServicePort);
         } else {
            Console.WriteLine("Failed to parse command line args.");
         }
      }
   }

   public class HydarEgg : INestApplicationEgg {
      private readonly List<object> keepalive = new List<object>();

      public NestResult Start(IEggParameters parameters) {
         throw new NotImplementedException();
      }

      public NestResult Start(int servicePort) {
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
            x.RegisterPortableObjectType(100006, typeof(LeaderRepartitionSignalDto));
            x.RegisterPortableObjectType(100007, typeof(CohortRepartitionCompletionDto));
            x.RegisterPortableObjectType(100008, typeof(CohortHeartbeatDto));
            x.RegisterPortableObjectType(100009, typeof(LeaderRepartitionCompletingDto));
            x.RegisterPortableObjectType(100010, typeof(CacheHaveDto));
         });
         var pofSerializer = new PofSerializer(pofContext);
         var pofStreamsFactory = new PofStreamsFactoryImpl(threadingProxy, streamFactory, pofSerializer);

         // Dargon.Services for node-to-node networking
         var serviceClientFactory = new ServiceClientFactory(new ProxyGenerator(), streamFactory, collectionFactory, threadingProxy, networkingProxy, pofSerializer, pofStreamsFactory);
         var clusteringConfiguration = new ClusteringConfiguration(servicePort, 1000, ClusteringRoleFlags.HostOnly);
         var serviceClient = serviceClientFactory.CreateOrJoin(clusteringConfiguration);
         keepalive.Add(serviceClient);

         // Dargon.Courier for clustered networking
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

         // Initialize Hydar Cache
         var cacheFactory = new CacheFactory(endpoint, messageSender, messageRouter, receivedMessageFactory, serviceClient);
         var client = new ClusterClient();
         client.AddCache(cacheFactory.Create<int, string>("test-cache"));

         new CountdownEvent(1).Wait();
         return NestResult.Success;
      }

      public NestResult Shutdown() {
         return NestResult.Success;
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
         writer.WriteCollection(1, Followers ?? new ItzWarty.Collections.HashSet<Guid>());
      }

      public void Deserialize(IPofReader reader) {
         Nominee = reader.ReadGuid(0);
         Followers = reader.ReadCollection<Guid, ItzWarty.Collections.HashSet<Guid>>(1);
      }
   }

   public class LeaderHeartbeatDto : IPortableObject {
      public LeaderHeartbeatDto() { }

      public LeaderHeartbeatDto(Guid epochId, IReadOnlySet<Guid> participants) {
         this.EpochId = epochId;
         this.Participants = participants;
      }

      public Guid EpochId { get; set; }
      public IReadOnlySet<Guid> Participants { get; set; }

      public void Serialize(IPofWriter writer) {
         writer.WriteGuid(0, EpochId);
         writer.WriteCollection(1, Participants);
      }

      public void Deserialize(IPofReader reader) {
         EpochId = reader.ReadGuid(0);
         Participants = reader.ReadCollection<Guid, ItzWarty.Collections.HashSet<Guid>>(1);
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

   public class CacheHaveDto : IPortableObject {
      public CacheHaveDto() { }

      public CacheHaveDto(PartitionBlockInterval[] blocks) {
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

   public class LeaderRepartitionSignalDto : IPortableObject {
      private Guid epochId;
      private IReadOnlySet<Guid> participants;

      public LeaderRepartitionSignalDto() { }

      public LeaderRepartitionSignalDto(Guid epochId, IReadOnlySet<Guid> participants) {
         this.epochId = epochId;
         this.participants = participants;
      }

      public Guid EpochId => epochId;
      public IReadOnlySet<Guid> Participants => participants;

      public void Serialize(IPofWriter writer) {
         writer.WriteGuid(0, epochId);
         writer.WriteCollection(1, participants);
      }
      public void Deserialize(IPofReader reader) {
         epochId = reader.ReadGuid(0);
         participants = reader.ReadCollection<Guid, ItzWarty.Collections.HashSet<Guid>>(1);
      }
   }

   public class CohortRepartitionCompletionDto : IPortableObject {
      public void Serialize(IPofWriter writer) { }
      public void Deserialize(IPofReader reader) { }
   }

   public class LeaderRepartitionCompletingDto : IPortableObject {
      public void Serialize(IPofWriter writer) { }
      public void Deserialize(IPofReader reader) { }
   }

   public class CohortHeartbeatDto : IPortableObject {
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
