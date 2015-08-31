using System.Collections.Generic;
using Dargon.Hydar.Cache.Data;
using Dargon.Hydar.Cache.PortableObjects;
using Dargon.Hydar.Common;
using NLog;

namespace Dargon.Hydar.Cache.Services {
   public class CacheServiceImpl<TKey, TValue> : CacheService<TKey, TValue> {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();
      private readonly CacheOperationsManager<TKey, TValue> cacheOperationsManager;

      public CacheServiceImpl(CacheOperationsManager<TKey, TValue> cacheOperationsManager) {
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

      public TResult ExecuteProxiedOperation<TResult>(EntryOperation<TKey, TValue, TResult> operation) {
         logger.Info("Executing Proxied Operation: " + operation);
         return cacheOperationsManager.EnqueueAndAwaitResults(operation).Result;
      }

      public TValue Get(TKey key) {
         var operation = new EntryOperationGet<TKey, TValue>(key);
         return cacheOperationsManager.EnqueueAndAwaitResults(operation).Result;
      }

      public bool Put(TKey key, TValue value) {
         var operation = new EntryOperationPut<TKey, TValue>(key, value);
         return cacheOperationsManager.EnqueueAndAwaitResults(operation).Result;
      }

      public TResult Process<TResult>(TKey key, EntryProcessor<TKey, TValue, TResult> entryProcessor) {
         var operation = new EntryOperationProcess<TKey, TValue, TResult>(key, entryProcessor);
         return cacheOperationsManager.EnqueueAndAwaitResults(operation).Result;
      }
   }
}