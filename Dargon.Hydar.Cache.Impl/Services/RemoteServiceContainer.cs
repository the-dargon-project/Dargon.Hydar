using System;
using System.Net;
using Dargon.Courier.Identities;
using Dargon.Courier.Peering;
using Dargon.Hydar.Cache.PortableObjects;
using Dargon.Services;
using ItzWarty.Collections;

namespace Dargon.Hydar.Cache.Services {
   public class RemoteServiceContainer<TKey, TValue> {
      private readonly CacheConfiguration<TKey, TValue> cacheConfiguration;
      private readonly IServiceClientFactory serviceClientFactory;
      private readonly ReadablePeerRegistry readablePeerRegistry;
      private readonly IConcurrentDictionary<IPEndPoint, IServiceClient> serviceClientsByOrigin;

      public RemoteServiceContainer(CacheConfiguration<TKey, TValue> cacheConfiguration, IServiceClientFactory serviceClientFactory, ReadablePeerRegistry readablePeerRegistry, IConcurrentDictionary<IPEndPoint, IServiceClient> serviceClientsByOrigin) {
         this.cacheConfiguration = cacheConfiguration;
         this.serviceClientFactory = serviceClientFactory;
         this.readablePeerRegistry = readablePeerRegistry;
         this.serviceClientsByOrigin = serviceClientsByOrigin;
      }

      public CacheService<TKey, TValue> GetCacheService(IPAddress address, int port) {
         var serviceClient = serviceClientsByOrigin.GetOrAdd(new IPEndPoint(address, port), add => ConstructServiceClient(address, port));
         return serviceClient.GetService<CacheService<TKey, TValue>>(cacheConfiguration.Guid);
      }

      private IServiceClient ConstructServiceClient(IPAddress address, int port) {
         IClusteringConfiguration clusteringConfiguration = new ClusteringConfiguration(address, port, ClusteringRoleFlags.GuestOnly);
         return serviceClientFactory.CreateOrJoin(clusteringConfiguration);
      }

      public CacheService<TKey, TValue> GetCacheService(Guid peerIdentifier) {
         var peerCourierEndpoint = readablePeerRegistry.GetRemoteCourierEndpointOrNull(peerIdentifier);
         var peerServiceDescriptor = peerCourierEndpoint.GetProperty<HydarServiceDescriptor>();
         return GetCacheService(peerCourierEndpoint.LastAddress, peerServiceDescriptor.ServicePort);
      }
   }
}
