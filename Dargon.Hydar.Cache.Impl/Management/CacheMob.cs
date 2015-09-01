using System;
using Dargon.Hydar.Cache.Data.Operations;
using Dargon.Management;

namespace Dargon.Hydar.Cache.Management {
   public class CacheMob<TKey, TValue> {
      private readonly CacheOperationsManager<TKey, TValue> cacheOperationsManager;

      public CacheMob(CacheOperationsManager<TKey, TValue> cacheOperationsManager) {
         this.cacheOperationsManager = cacheOperationsManager;
      }

      [ManagedOperation]
      public string Hello() => "Hello, world!";

      [ManagedOperation]
      public TValue Get(TKey key) {
         try {
            var operation = new EntryOperationGet<TKey, TValue>(key);
            return cacheOperationsManager.EnqueueAndAwaitResults(operation).Result;
         } catch (Exception e) {
            Console.WriteLine(e);
            return default(TValue);
         }
      }

      [ManagedOperation]
      public bool Put(TKey key, TValue value) {
         try {
            var operation = new EntryOperationPut<TKey, TValue>(key, value);
            return cacheOperationsManager.EnqueueAndAwaitResults(operation).Result;
         } catch (Exception e) {
            Console.WriteLine(e);
            return false;
         }
      }
   }
}