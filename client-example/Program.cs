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
using System.Threading.Tasks;
using Dargon.Hydar.Common;
using NLog;
using SCG = System.Collections.Generic;

namespace client_example {
   public static class Program {
      private static Logger logger = LogManager.GetCurrentClassLogger();

      public static void Main(string[] args) {
         var ryu = new RyuFactory().Create();
         ryu.Touch<ItzWartyProxiesRyuPackage>();
         ryu.Setup();

         new Thread(() => Dargon.Hydar.Program.Main("-s 32001 -m 32101".Split(' '))).Start();
         new Thread(() => Dargon.Hydar.Program.Main("-s 32002 -m 32102".Split(' '))).Start();
         new Thread(() => Dargon.Hydar.Program.Main("-s 32003 -m 32103".Split(' '))).Start();
         new Thread(() => Dargon.Hydar.Program.Main("-s 32004 -m 32104".Split(' '))).Start();

         var cacheName = "test-cache";
         var guidHelper = new GuidHelperImpl();
         var cacheGuid = guidHelper.ComputeMd5(cacheName);

         Thread.Sleep(5000);
         logger.Info("Running client operations.");
         var serviceClientFactory = ryu.Get<IServiceClientFactory>();
         var serviceClients = Util.Generate(4, i => serviceClientFactory.CreateOrJoin(new ClusteringConfiguration(32001 + i, 0, ClusteringRoleFlags.GuestOnly)));
         var cacheServices = Util.Generate(serviceClients.Length, i => serviceClients[i].GetService<CacheRoot<int, int>.ClientCacheService>(cacheGuid));

         var proxyGenerator = ryu.Get<ProxyGenerator>();
         var cache = new ClientCacheImpl<int, int>(proxyGenerator.CreateInterfaceProxyWithoutTarget<CacheRoot<int, int>.ClientCacheService>(new RoundRobinServiceProxyInterceptorImpl<CacheRoot<int, int>.ClientCacheService>(cacheServices)));

         cache[0] = 1337;
         for (var i = 0; i < 10; i++) {
            logger.Info("Reading from cache: " + cache[0]);
            cache.Process(0, new Int32IncrementProcessor<int>());
         }

         cache[0] = 1337;
         var increments = 100;
         var incrementCountdown = new CountdownEvent(increments);
         for (var i = 0; i < increments; i++) {
            Task.Factory.StartNew(() => {
               cache.Process(0, new Int32IncrementProcessor<int>());
               incrementCountdown.Signal();
            }, TaskCreationOptions.LongRunning);
         }
         incrementCountdown.Wait();
         for (var i = 0; i < 4; i++) {
            logger.Info("Reading from cache " + i + ": " + cache[0]);
         }

         var latch = new CountdownEvent(1);
         latch.Wait();
      }

      public class RoundRobinServiceProxyInterceptorImpl<TService> : IInterceptor {
         private static Logger logger = LogManager.GetCurrentClassLogger();

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
            logger.Trace("Round Robin Dispatch #" + count);
            invocation.ReturnValue = invocation.Method.Invoke(candidate, invocation.Arguments);
         }
      }

      public class ClientCacheImpl<TKey, TValue> : Cache<TKey, TValue> {
         private readonly CacheRoot<TKey, TValue>.ClientCacheService cacheService;

         public ClientCacheImpl(CacheRoot<TKey, TValue>.ClientCacheService cacheService) {
            this.cacheService = cacheService;
         }

         public TResult Process<TResult>(TKey key, EntryProcessor<TKey, TValue, TResult> entryProcessor) {
            return cacheService.Process(key, entryProcessor);
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
