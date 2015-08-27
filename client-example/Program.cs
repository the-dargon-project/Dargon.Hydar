using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Hydar;
using Dargon.Hydar.Utilities;
using Dargon.Ryu;
using Dargon.Services;
using ItzWarty;

namespace client_example {
   class Program {
      public static void Main(string[] args) {
         new Thread(() => Dargon.Hydar.Program.Main("-s 32001 -m 32101".Split(' '))).Start();
         new Thread(() => Dargon.Hydar.Program.Main("-s 32002 -m 32102".Split(' '))).Start();
         new Thread(() => Dargon.Hydar.Program.Main("-s 32003 -m 32103".Split(' '))).Start();
         new Thread(() => Dargon.Hydar.Program.Main("-s 32004 -m 32104".Split(' '))).Start();

         var ryu = new RyuFactory().Create();
         ryu.Setup();

         var cacheName = "test-cache";
         var guidHelper = new GuidHelperImpl();
         var cacheGuid = guidHelper.ComputeMd5(cacheName);

         Thread.Sleep(5000);
         Console.WriteLine("Running client operations.");
         var serviceClientFactory = ryu.Get<IServiceClientFactory>();
         var serviceClients = Util.Generate(4, i => serviceClientFactory.CreateOrJoin(new ClusteringConfiguration(32001 + i, 0, ClusteringRoleFlags.GuestOnly)));
         var cacheServices = Util.Generate(serviceClients.Length, i => serviceClients[i].GetService<CacheRoot<int, string>.ClientCacheService>(cacheGuid));
         cacheServices[0].Put(0, "test");
         Console.WriteLine("Reading from cache: " + cacheServices[1].Get(0));

         var latch = new CountdownEvent(1);
         latch.Wait();
      }
   }
}
