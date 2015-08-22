using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.Identities;
using Dargon.Courier.Peering;
using Dargon.PortableObjects;
using Dargon.Services;
using ItzWarty.Collections;
using NLog;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
      public class RemoteServiceContainer {
         private readonly CacheConfiguration cacheConfiguration;
         private readonly IServiceClientFactory serviceClientFactory;
         private readonly ReadablePeerRegistry readablePeerRegistry;
         private readonly IConcurrentDictionary<IPEndPoint, IServiceClient> serviceClientsByOrigin;

         public RemoteServiceContainer(CacheConfiguration cacheConfiguration, IServiceClientFactory serviceClientFactory, ReadablePeerRegistry readablePeerRegistry, IConcurrentDictionary<IPEndPoint, IServiceClient> serviceClientsByOrigin) {
            this.cacheConfiguration = cacheConfiguration;
            this.serviceClientFactory = serviceClientFactory;
            this.readablePeerRegistry = readablePeerRegistry;
            this.serviceClientsByOrigin = serviceClientsByOrigin;
         }

         public CacheService GetCacheService(IPAddress address, int port) {
            var serviceClient = serviceClientsByOrigin.GetOrAdd(new IPEndPoint(address, port), add => ConstructServiceClient(address, port));
            return serviceClient.GetService<CacheService>(cacheConfiguration.Guid);
         }

         private IServiceClient ConstructServiceClient(IPAddress address, int port) {
            IClusteringConfiguration clusteringConfiguration = new ClusteringConfiguration(address, port, ClusteringRoleFlags.GuestOnly);
            return serviceClientFactory.CreateOrJoin(clusteringConfiguration);
         }

         public CacheService GetCacheService(Guid peerIdentifier) {
            var peerCourierEndpoint = readablePeerRegistry.GetRemoteCourierEndpointOrNull(peerIdentifier);
            var peerServiceDescriptor = peerCourierEndpoint.GetProperty<HydarServiceDescriptor>();
            return GetCacheService(peerCourierEndpoint.LastAddress, peerServiceDescriptor.ServicePort);
         }
      }

      public interface ClientCacheService {
         bool Put(TKey key, TValue value);
      }

      public interface InterCacheService {
         BlockTransferResult TransferBlocks(PartitionBlockInterval[] blockIntervals);
         TResult ExecuteProxiedOperation<TResult>(EntryOperation<TResult> operation);
      }

      public interface CacheService : ClientCacheService, InterCacheService { }

      public class CacheServiceImpl : CacheService {
         private static readonly Logger logger = LogManager.GetCurrentClassLogger();
         private readonly CacheOperationsManager cacheOperationsManager;

         public CacheServiceImpl(CacheOperationsManager cacheOperationsManager) {
            this.cacheOperationsManager = cacheOperationsManager;
         }

         public BlockTransferResult TransferBlocks(PartitionBlockInterval[] blockIntervals) {
            var result = new Dictionary<uint, object>();
            foreach (var interval in blockIntervals) {
               for (var blockId = interval.StartBlockInclusive; blockId < interval.EndBlockExclusive; blockId++) {
                  result.Add(blockId, new object());
               }
            }
            return new BlockTransferResult(result);
         }

         public TResult ExecuteProxiedOperation<TResult>(EntryOperation<TResult> operation) {
            logger.Info("Executing Proxied Operation: " + operation);
            return cacheOperationsManager.EnqueueAndAwaitResults(operation).Result;
         }

         public bool Put(TKey key, TValue value) {
            throw new NotImplementedException();
         }
      }
   }
}
