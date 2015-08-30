using Castle.DynamicProxy;
using CommandLine;
using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using Dargon.Courier.Networking;
using Dargon.Courier.Peering;
using Dargon.Courier.PortableObjects;
using Dargon.Management;
using Dargon.Management.Server;
using Dargon.Nest.Egg;
using Dargon.PortableObjects;
using Dargon.PortableObjects.Streams;
using Dargon.Services;
using Dargon.Services.Messaging;
using ItzWarty;
using ItzWarty.Collections;
using ItzWarty.IO;
using ItzWarty.Networking;
using ItzWarty.Pooling;
using ItzWarty.Threading;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Dargon.Courier;
using Dargon.Hydar.Common;
using Dargon.Ryu;
using MessageSenderImpl = Dargon.Courier.Messaging.MessageSenderImpl;

namespace Dargon.Hydar {
   public static class Program {
      public class Options {
         [Option('s', "ServicePort", DefaultValue = 32000, HelpText = "Dedicated port for hydar cache service dsp.")]
         public int ServicePort { get; set; }

         [Option('m', "ManagementPort", DefaultValue = 32002, HelpText = "Dedicated port for hydar cache service dmi.")]
         public int ManagementPort { get; set; }
      }

      public static void Main(string[] args) {
         ThreadPool.SetMaxThreads(64, 64);
         InitializeLogging();

         Console.Title = "PID " + Process.GetCurrentProcess().Id;
         var options = new Options();
         if (Parser.Default.ParseArgumentsStrict(args, options)) {
            new HydarEgg().Start(options.ServicePort, options.ManagementPort);
         } else {
            Console.WriteLine("Failed to parse command line args.");
         }
      }

      private static void InitializeLogging() {
         var config = new LoggingConfiguration();
         Target debuggerTarget = new DebuggerTarget() {
            Layout = "${longdate}|${level}|${logger}|${message} ${exception:format=tostring}"
         };
         Target consoleTarget = new ColoredConsoleTarget() {
            Layout = "${longdate}|${level}|${logger}|${message} ${exception:format=tostring}"
         };

#if !DEBUG
         debuggerTarget = new AsyncTargetWrapper(debuggerTarget);
         consoleTarget = new AsyncTargetWrapper(consoleTarget);
#else
         AsyncTargetWrapper a; // Placeholder for optimizing imports
#endif

         config.AddTarget("debugger", debuggerTarget);
         config.AddTarget("console", consoleTarget);

         var debuggerRule = new LoggingRule("*", LogLevel.Trace, debuggerTarget);
         config.LoggingRules.Add(debuggerRule);

         var consoleRule = new LoggingRule("*", LogLevel.Trace, consoleTarget);
         config.LoggingRules.Add(consoleRule);

         LogManager.Configuration = config;
      }
   }

   public class HydarEgg : INestApplicationEgg {
      private readonly List<object> keepalive = new List<object>();
      private readonly RyuContainer ryu;

      public HydarEgg() {
         ryu = new RyuFactory().Create();
      }

      public NestResult Start(IEggParameters parameters) {
         throw new NotImplementedException();
      }

      public NestResult Start(int servicePort, int managementPort) {
         ryu.Setup();

         var networkingProxy = ryu.Get<INetworkingProxy>();

         // Dargon.Management
         var managementServerEndpoint = networkingProxy.CreateAnyEndPoint(managementPort);
         var managementFactory = ryu.Get<ManagementFactoryImpl>();
         var localManagementServer = managementFactory.CreateServer(new ManagementServerConfiguration(managementServerEndpoint));
         ryu.Set<ILocalManagementServer>(localManagementServer);
         keepalive.Add(localManagementServer);

         // Dargon.Services for node-to-node networking
         var clusteringConfiguration = new ClusteringConfiguration(servicePort, 1000, ClusteringRoleFlags.HostOnly);
         ryu.Set<IClusteringConfiguration>(clusteringConfiguration);
         var serviceClient = ryu.Get<IServiceClient>();
         keepalive.Add(serviceClient);

         // Initialize Dargon.Courier
         var courierPort = 50555;
         var courierClientFactory = ryu.Get<CourierClientFactory>();
         var courierClient = courierClientFactory.CreateUdpCourierClient(courierPort);
         ryu.Set<CourierClient>(courierClient);
         
         // Dargon.Courier for clustered networking
         Console.Title = "PID " + Process.GetCurrentProcess().Id + ": " + courierClient.Identifier.ToString("N");

         // Initialize Hydar Cache
         var cacheFactory = ryu.Get<CacheFactory>();
         cacheFactory.SetServicePort(servicePort);
         var client = new ClusterClient();
         client.AddCache(cacheFactory.Create<int, int>("test-cache"));

         new CountdownEvent(1).Wait();
         return NestResult.Success;
      }

      public NestResult Shutdown() {
         return NestResult.Success;
      }
   }

   [Guid("07B72FFE-828A-4A20-BA83-06B60D990B30")]
   public class HydarServiceDescriptor : IPortableObject {
      private const int kVersion = 0;

      public int ServicePort { get; set; }

      public void Serialize(IPofWriter writer) {
         writer.WriteS32(0, kVersion);
         writer.WriteS32(1, ServicePort);
      }

      public void Deserialize(IPofReader reader) {
         var version = reader.ReadS32(0);
         ServicePort = reader.ReadS32(1);

         Trace.Assert(version == kVersion, "version == kVersion");
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

      public LeaderHeartbeatDto(Guid epochId, Guid[] orderedParticipants) {
         this.EpochId = epochId;
         this.OrderedParticipants = orderedParticipants;
      }

      public Guid EpochId { get; set; }
      public Guid[] OrderedParticipants { get; set; }

      public void Serialize(IPofWriter writer) {
         writer.WriteGuid(0, EpochId);
         writer.WriteCollection(1, OrderedParticipants);
      }

      public void Deserialize(IPofReader reader) {
         EpochId = reader.ReadGuid(0);
         OrderedParticipants = reader.ReadArray<Guid>(1);
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

      public CacheHaveDto(PartitionBlockInterval[] blocks, int servicePort) {
         Blocks = blocks;
         ServicePort = servicePort;
      }

      public PartitionBlockInterval[] Blocks { get; set; }
      public int ServicePort { get; set; }

      public void Serialize(IPofWriter writer) {
         writer.WriteCollection(0, Blocks);
         writer.WriteS32(1, ServicePort);
      }

      public void Deserialize(IPofReader reader) {
         Blocks = reader.ReadArray<PartitionBlockInterval>(0);
         ServicePort = reader.ReadS32(1);
      }
   }

   public class OutsiderAnnounceDto : IPortableObject {
      public void Serialize(IPofWriter writer) { }
      public void Deserialize(IPofReader reader) { }
   }

   public class LeaderRepartitionSignalDto : IPortableObject {
      private Guid epochId;
      private Guid[] participantsOrdered;

      public LeaderRepartitionSignalDto() { }

      public LeaderRepartitionSignalDto(Guid epochId, Guid[] participantsOrdered) {
         this.epochId = epochId;
         this.participantsOrdered = participantsOrdered;
      }

      public Guid EpochId => epochId;
      public Guid[] ParticipantsOrdered => participantsOrdered;

      public void Serialize(IPofWriter writer) {
         writer.WriteGuid(0, epochId);
         writer.WriteCollection(1, participantsOrdered);
      }
      public void Deserialize(IPofReader reader) {
         epochId = reader.ReadGuid(0);
         participantsOrdered = reader.ReadArray<Guid>(1);
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

   public class BlockTransferResult : IPortableObject {
      private IDictionary<uint, object> blocks;

      public BlockTransferResult() { }

      public BlockTransferResult(IDictionary<uint, object> blocks) {
         this.blocks = blocks;
      }

      public IReadOnlyDictionary<uint, object> Blocks => (IReadOnlyDictionary<uint, object>)blocks;

      public void Serialize(IPofWriter writer) {
         writer.WriteMap(0, blocks);
      }

      public void Deserialize(IPofReader reader) {
         blocks = reader.ReadMap<uint, object>(0);
      }
   }
}
