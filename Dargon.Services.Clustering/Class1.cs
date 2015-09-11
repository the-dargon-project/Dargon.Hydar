using Castle.DynamicProxy;
using ItzWarty.Collections;
using System;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;

namespace Dargon.Services.Clustering {
   internal static class AttributeUtilities {
      private static readonly AttributeUtilitiesInterface instance = new AttributeUtilitiesImpl();

      public static bool TryGetInterfaceGuid(Type interfaceType, out Guid guid) {
         return instance.TryGetInterfaceGuid(interfaceType, out guid);
      }
   }

   public interface AttributeUtilitiesInterface {
      bool TryGetInterfaceGuid(Type interfaceType, out Guid guid);
   }

   public class AttributeUtilitiesImpl : AttributeUtilitiesInterface {
      public bool TryGetInterfaceGuid(Type interfaceType, out Guid guid) {
         var attributes = interfaceType.GetCustomAttributes(typeof(GuidAttribute), false);
         if (attributes.Any()) {
            guid = Guid.Parse(((GuidAttribute)attributes.First()).Value);
            return true;
         } else {
            guid = Guid.Empty;
            return false;
         }
      }
   }

   public class RemoteClusterServiceClientImpl : IServiceClient {
      private readonly RemoteServiceProxyContainer remoteServiceProxyContainer;

      public RemoteClusterServiceClientImpl(RemoteServiceProxyContainer remoteServiceProxyContainer) {
         this.remoteServiceProxyContainer = remoteServiceProxyContainer;
      }

      public TService GetService<TService>() where TService : class => remoteServiceProxyContainer.GetService<TService>();
      public TService GetService<TService>(Guid serviceGuid) where TService : class => remoteServiceProxyContainer.GetService<TService>(serviceGuid);

      public void RegisterService(object serviceImplementation, Type serviceInterface, Guid serviceGuid) => ThrowForServiceRegistry();
      public void UnregisterService(object serviceImplementation, Guid serviceGuid) => ThrowForServiceRegistry();
      public void RegisterService(object serviceImplementation, Type serviceInterface) => ThrowForServiceRegistry();
      public void UnregisterService(object serviceImplementation, Type serviceInterface) => ThrowForServiceRegistry();

      private void ThrowForServiceRegistry() {
         throw new InvalidOperationException("Remote cluster service client does not support service registration!");
      }
   }

   public interface RemoteServiceClientsSource {
      IServiceClient[] ServiceClients { get; }
   }

   public class RemoteClusterServiceProxyContainerImpl : RemoteServiceProxyContainer, RemoteServiceClientsSource {
      private readonly ProxyGenerator proxyGenerator;
      private readonly IConcurrentDictionary<Guid, object> serviceProxiesByGuid = new ConcurrentDictionary<Guid, object>();
      private readonly IConcurrentDictionary<IPEndPoint, IServiceClient> remoteServiceClientsByIpEndpoint;
      private readonly IServiceClient[] serviceClients;

      public RemoteClusterServiceProxyContainerImpl(ProxyGenerator proxyGenerator, IConcurrentDictionary<IPEndPoint, IServiceClient> initialRemoteServiceClientsByIpEndpoint) {
         this.proxyGenerator = proxyGenerator;
         this.remoteServiceClientsByIpEndpoint = initialRemoteServiceClientsByIpEndpoint;
         this.serviceClients = remoteServiceClientsByIpEndpoint.Values.ToArray();
      }

      public IServiceClient[] ServiceClients => serviceClients;

      public TService GetService<TService>() where TService : class {
         Guid serviceGuid;
         var serviceInterface = typeof(TService);
         if (!AttributeUtilities.TryGetInterfaceGuid(serviceInterface, out serviceGuid)) {
            throw new ArgumentException($"Service Interface {serviceInterface.FullName} does not expose Guid Attribute!");
         } else {
            return GetService<TService>(serviceGuid);
         }
      }

      public TService GetService<TService>(Guid serviceGuid) where TService : class {
         return (TService)serviceProxiesByGuid.GetOrAdd(serviceGuid, ConstructServiceProxy<TService>);
      }

      private TService ConstructServiceProxy<TService>(Guid serviceGuid) where TService : class {
         var interceptor = new RoundRobinServiceProxyInterceptorImpl<TService>(this, serviceGuid);
         return proxyGenerator.CreateInterfaceProxyWithoutTarget<TService>(interceptor);
      }
   }

   public class RoundRobinServiceProxyInterceptorImpl<TService> : IInterceptor where TService : class {
      private readonly object updateSynchronization = new object();
      private readonly RemoteServiceClientsSource remoteServiceClientsSource;
      private readonly Guid serviceGuid;
      private IServiceClient[] previousServiceClients = null;
      private TService[] services = null;
      private int counter = 0;

      public RoundRobinServiceProxyInterceptorImpl(RemoteServiceClientsSource remoteServiceClientsSource, Guid serviceGuid) {
         this.remoteServiceClientsSource = remoteServiceClientsSource;
         this.serviceGuid = serviceGuid;
      }

      public void Intercept(IInvocation invocation) {
         SynchronizeServices();

         var count = Interlocked.Increment(ref counter);
         var candidates = services;
         var candidate = candidates[count % candidates.Length];
         invocation.ReturnValue = invocation.Method.Invoke(candidate, invocation.Arguments);
      }

      private void SynchronizeServices() {
         var currentServiceClients = remoteServiceClientsSource.ServiceClients;
         if (currentServiceClients != previousServiceClients) {
            lock (updateSynchronization) {
               currentServiceClients = remoteServiceClientsSource.ServiceClients;
               if (currentServiceClients != previousServiceClients) {
                  previousServiceClients = currentServiceClients;
                  services = currentServiceClients.Select(x => x.GetService<TService>()).ToArray();
               }
            }
         }
      }
   }
}
