using Castle.DynamicProxy;
using Dargon.Hydar;
using Dargon.Hydar.Client;
using Dargon.Hydar.Utilities;
using Dargon.Ryu;
using Dargon.Services;
using ItzWarty;
using ItzWarty.Collections;
using System;
using System.Linq;
using System.Threading;
using SCG = System.Collections.Generic;

namespace client_example {
   class Program {
      public static void Main(string[] args) {
         var ryu = new RyuFactory().Create();
         ryu.Setup();

         new Thread(() => Dargon.Hydar.Program.Main("-s 32001 -m 32101".Split(' '))).Start();
         new Thread(() => Dargon.Hydar.Program.Main("-s 32002 -m 32102".Split(' '))).Start();
         new Thread(() => Dargon.Hydar.Program.Main("-s 32003 -m 32103".Split(' '))).Start();
         new Thread(() => Dargon.Hydar.Program.Main("-s 32004 -m 32104".Split(' '))).Start();

         var cacheName = "test-cache";
         var guidHelper = new GuidHelperImpl();
         var cacheGuid = guidHelper.ComputeMd5(cacheName);

         Thread.Sleep(5000);
         Console.WriteLine("Running client operations.");
         var serviceClientFactory = ryu.Get<IServiceClientFactory>();
         var serviceClients = Util.Generate(4, i => serviceClientFactory.CreateOrJoin(new ClusteringConfiguration(32001 + i, 0, ClusteringRoleFlags.GuestOnly)));
         var cacheServices = Util.Generate(serviceClients.Length, i => serviceClients[i].GetService<CacheRoot<int, string>.ClientCacheService>(cacheGuid));

         var proxyGenerator = ryu.Get<ProxyGenerator>();
         var cache = new ClientCacheImpl<int, string>(proxyGenerator.CreateInterfaceProxyWithoutTarget<CacheRoot<int, string>.ClientCacheService>(new RoundRobinServiceProxyInterceptorImpl<CacheRoot<int, string>.ClientCacheService>(cacheServices)));


         cache[0] = "test";
         for (var i = 0; i < 10; i++) {
            Console.WriteLine("Reading from cache: " + cache[0]);
         }

         var latch = new CountdownEvent(1);
         latch.Wait();
      }

      public class RoundRobinServiceProxyInterceptorImpl<TService> : IInterceptor {
         private readonly object synchronization = new object();
         private int counter = 0;
         private TService[] services;

         public RoundRobinServiceProxyInterceptorImpl(TService[] initialServices) {
            services = initialServices.ToArray();
         }

         public void AddService(TService service) {
            lock (synchronization) {
               var nextServices = new HashSet<TService>(services);
               nextServices.Add(service);
               services = nextServices.ToArray();
            }
         }

         public void RemoveService(TService service) {
            lock (synchronization) {
               var nextServices = new HashSet<TService>(services);
               nextServices.Remove(service);
               services = nextServices.ToArray();
            }
         }

         public void Intercept(IInvocation invocation) {
            var count = Interlocked.Increment(ref counter);
            var candidates = services;
            var candidate = candidates[count % candidates.Length];
            Console.WriteLine("Round Robin Dispatch #" + count);
            invocation.ReturnValue = invocation.Method.Invoke(candidate, invocation.Arguments);
         }
      }

      public class ClientCacheImpl<TKey, TValue> : Cache<TKey, TValue> {
         private readonly CacheRoot<TKey, TValue>.ClientCacheService cacheService;

         public ClientCacheImpl(CacheRoot<TKey, TValue>.ClientCacheService cacheService) {
            this.cacheService = cacheService;
         }

         public TResult Process<TResult>(TKey key, EntryProcessor<TKey, TValue, TResult> entryProcessor) {
            throw new NotImplementedException();
         }

         public TResult Process<TResult, TProcessor>(TKey key, params object[] args) where TProcessor : EntryProcessor<TKey, TValue, TResult> {
            var entryProcessor = (TProcessor)Activator.CreateInstance(typeof(TProcessor), args);
            return Process(key, entryProcessor);
         }

         public bool TryGetValue(TKey key, out TValue value) {
            throw new NotImplementedException();
         }

         public bool ContainsKey(TKey key) {
            throw new NotImplementedException();
         }

         public TValue this[TKey key] { get { return cacheService.Get(key); } set { cacheService.Put(key, value); } }
      }
   }
}
