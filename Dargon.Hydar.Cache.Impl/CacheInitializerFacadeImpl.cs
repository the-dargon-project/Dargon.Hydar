using Dargon.Courier;
using Dargon.Courier.Identities;
using Dargon.Hydar.Cache.Data.Storage;
using Dargon.Hydar.Cache.PortableObjects;

namespace Dargon.Hydar.Cache {
   public class CacheInitializerFacadeImpl : CacheInitializerFacade {
      private readonly HydarConfiguration hydarConfiguration;
      private readonly HydarNetworkingResources hydarNetworkingResources;
      private readonly CacheFactory cacheFactory;
      private readonly CacheDispatcher cacheDispatcher;

      public CacheInitializerFacadeImpl(HydarConfiguration hydarConfiguration, HydarNetworkingResources hydarNetworkingResources, CacheFactory cacheFactory, CacheDispatcher cacheDispatcher) {
         this.hydarConfiguration = hydarConfiguration;
         this.hydarNetworkingResources = hydarNetworkingResources;
         this.cacheFactory = cacheFactory;
         this.cacheDispatcher = cacheDispatcher;
      }

      public void Initialize() {
         var courierClient = hydarNetworkingResources.LocalCourierClient;
         courierClient.SetProperty(new HydarServiceDescriptor { ServicePort = hydarConfiguration.ServicePort });
      }

      public Cache<TKey, TValue> NearCache<TKey, TValue>(string name, CacheStore<TKey, TValue> cacheStore, CacheStorageStrategy cacheStorageStrategy) {
         var cacheConfiguration = new CacheConfigurationImpl<TKey, TValue> {
            Name = name,
            Storage = cacheStore,
            StorageStrategy = cacheStorageStrategy,
            ServicePort = hydarConfiguration.ServicePort
         };
         var cacheRoot = cacheFactory.Create(cacheConfiguration);
         cacheDispatcher.RegisterCache(cacheRoot);
         return cacheRoot;
      }
   }
}