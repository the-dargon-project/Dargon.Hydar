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
         var cacheDispatcher = new CacheDispatcher(courierClient.MessageRouter);
         cacheDispatcher.Initialize();
         cacheDispatcher.AddCache(cacheFactory.Create<int, int>("test-cache"));
         cacheDispatcher.AddCache(cacheFactory.Create<int, string>("test-string-cache"));

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

   public abstract class HydarCacheMessageBase : IPortableObject {
      private Guid cacheId;

      protected HydarCacheMessageBase(Guid cacheId) {
         this.cacheId = cacheId;
      }

      public Guid CacheId => cacheId;

      public void Serialize(IPofWriter writer) {
         writer.WriteGuid(0, cacheId);
         Serialize(writer, 1);
      }

      protected abstract void Serialize(IPofWriter writer, int baseSlot);

      public void Deserialize(IPofReader reader) {
         cacheId = reader.ReadGuid(0);
         Deserialize(reader, 1);
      }

      protected abstract void Deserialize(IPofReader reader, int baseSlot);
   }

   public class ElectionVoteDto : HydarCacheMessageBase {
      public ElectionVoteDto() : base(Guid.Empty) { }

      public ElectionVoteDto(Guid cacheId, Guid nominee, IReadOnlySet<Guid> followers) : base(cacheId) {
         this.Nominee = nominee;
         this.Followers = followers;
      }

      public Guid Nominee { get; set; }
      public IReadOnlySet<Guid> Followers { get; set; }

      protected override void Serialize(IPofWriter writer, int baseSlot) {
         writer.WriteGuid(baseSlot + 0, Nominee);
         writer.WriteCollection(baseSlot + 1, Followers ?? new ItzWarty.Collections.HashSet<Guid>());
      }

      protected override void Deserialize(IPofReader reader, int baseSlot) {
         Nominee = reader.ReadGuid(baseSlot + 0);
         Followers = reader.ReadCollection<Guid, ItzWarty.Collections.HashSet<Guid>>(baseSlot + 1);
      }
   }

   public class LeaderHeartbeatDto : HydarCacheMessageBase {
      public LeaderHeartbeatDto() : base(Guid.Empty) { }

      public LeaderHeartbeatDto(Guid cacheId, Guid epochId, Guid[] orderedParticipants) : base(cacheId) {
         this.EpochId = epochId;
         this.OrderedParticipants = orderedParticipants;
      }

      public Guid EpochId { get; set; }
      public Guid[] OrderedParticipants { get; set; }

      protected override void Serialize(IPofWriter writer, int baseSlot) {
         writer.WriteGuid(baseSlot + 0, EpochId);
         writer.WriteCollection(baseSlot + 1, OrderedParticipants);
      }

      protected override void Deserialize(IPofReader reader, int baseSlot) {
         EpochId = reader.ReadGuid(baseSlot + 0);
         OrderedParticipants = reader.ReadArray<Guid>(baseSlot + 1);
      }
   }

   public class CacheNeedDto : HydarCacheMessageBase {
      public CacheNeedDto() : base(Guid.Empty) { }

      public CacheNeedDto(Guid cacheId, PartitionBlockInterval[] blocks) : base(cacheId) {
         Blocks = blocks;
      }

      public PartitionBlockInterval[] Blocks { get; set; }

      protected override void Serialize(IPofWriter writer, int baseSlot) {
         writer.WriteCollection(baseSlot + 0, Blocks);
      }

      protected override void Deserialize(IPofReader reader, int baseSlot) {
         Blocks = reader.ReadArray<PartitionBlockInterval>(baseSlot + 0);
      }
   }

   public class CacheHaveDto : HydarCacheMessageBase {
      public CacheHaveDto() : base(Guid.Empty) { }

      public CacheHaveDto(Guid cacheId, PartitionBlockInterval[] blocks, int servicePort) : base(cacheId) {
         Blocks = blocks;
         ServicePort = servicePort;
      }

      public PartitionBlockInterval[] Blocks { get; set; }
      public int ServicePort { get; set; }

      protected override void Serialize(IPofWriter writer, int baseSlot) {
         writer.WriteCollection(baseSlot + 0, Blocks);
         writer.WriteS32(baseSlot + 1, ServicePort);
      }

      protected override void Deserialize(IPofReader reader, int baseSlot) {
         Blocks = reader.ReadArray<PartitionBlockInterval>(baseSlot + 0);
         ServicePort = reader.ReadS32(baseSlot + 1);
      }
   }

   public class OutsiderAnnounceDto : HydarCacheMessageBase {
      public OutsiderAnnounceDto() : base(Guid.Empty) { }

      public OutsiderAnnounceDto(Guid cacheId) : base(cacheId) { }

      protected override void Serialize(IPofWriter writer, int baseSlot) { }

      protected override void Deserialize(IPofReader reader, int baseSlot) { }
   }

   public class LeaderRepartitionSignalDto : HydarCacheMessageBase {
      private Guid epochId;
      private Guid[] participantsOrdered;

      public LeaderRepartitionSignalDto() : base(Guid.Empty) { }

      public LeaderRepartitionSignalDto(Guid cacheId, Guid epochId, Guid[] participantsOrdered) : base(cacheId) {
         this.epochId = epochId;
         this.participantsOrdered = participantsOrdered;
      }

      public Guid EpochId => epochId;
      public Guid[] ParticipantsOrdered => participantsOrdered;

      protected override void Serialize(IPofWriter writer, int baseSlot) {
         writer.WriteGuid(baseSlot + 0, epochId);
         writer.WriteCollection(baseSlot + 1, participantsOrdered);
      }

      protected override void Deserialize(IPofReader reader, int baseSlot) {
         epochId = reader.ReadGuid(baseSlot + 0);
         participantsOrdered = reader.ReadArray<Guid>(baseSlot + 1);
      }
   }

   public class CohortRepartitionCompletionDto : HydarCacheMessageBase {
      public CohortRepartitionCompletionDto() : base(Guid.Empty) { }

      public CohortRepartitionCompletionDto(Guid cacheId) : base(cacheId) { }

      protected override void Serialize(IPofWriter writer, int baseSlot) { }

      protected override void Deserialize(IPofReader reader, int baseSlot) { }
   }

   public class LeaderRepartitionCompletingDto : HydarCacheMessageBase {
      public LeaderRepartitionCompletingDto() : base(Guid.Empty) { }

      public LeaderRepartitionCompletingDto(Guid cacheId) : base(cacheId) { }

      protected override void Serialize(IPofWriter writer, int baseSlot) { }

      protected override void Deserialize(IPofReader reader, int baseSlot) { }
   }

   public class CohortHeartbeatDto : HydarCacheMessageBase {
      public CohortHeartbeatDto() : base(Guid.Empty) { }

      public CohortHeartbeatDto(Guid cacheId) : base(cacheId) { }

      protected override void Serialize(IPofWriter writer, int baseSlot) { }

      protected override void Deserialize(IPofReader reader, int baseSlot) { }
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
