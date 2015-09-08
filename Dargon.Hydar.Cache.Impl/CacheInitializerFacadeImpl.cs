using Dargon.Courier;
using Dargon.Courier.Identities;
using Dargon.Hydar.Cache.Data.Storage;
using Dargon.Hydar.Cache.PortableObjects;

namespace Dargon.Hydar.Cache {
   public class CacheInitializerFacadeImpl : CacheInitializerFacade {
      private readonly CourierClient courierClient;
      private readonly CacheFactory cacheFactory;
      private readonly CacheDispatcher cacheDispatcher;
      private int servicePort;

      public CacheInitializerFacadeImpl(CourierClient courierClient, CacheFactory cacheFactory, CacheDispatcher cacheDispatcher) {
         this.courierClient = courierClient;
         this.cacheFactory = cacheFactory;
         this.cacheDispatcher = cacheDispatcher;
      }

      public void SetServicePort(int newServicePort) {
         this.servicePort = newServicePort;
         courierClient.SetProperty(new HydarServiceDescriptor { ServicePort = servicePort });
      }

      public Cache<TKey, TValue> NearCache<TKey, TValue>(string name, CacheStore<TKey, TValue> cacheStore, CacheStorageStrategy cacheStorageStrategy) {
         var cacheConfiguration = new CacheConfigurationImpl<TKey, TValue> {
            Name = name,
            Storage = cacheStore,
            StorageStrategy = cacheStorageStrategy,
            ServicePort = servicePort
         };
         var cacheRoot = cacheFactory.Create(cacheConfiguration);
         cacheDispatcher.RegisterCache(cacheRoot);
         return cacheRoot;
      }
   }
}