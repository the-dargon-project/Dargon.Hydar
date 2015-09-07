using System.Threading.Tasks;
using Dargon.Hydar.Cache.Data.Operations;
using Dargon.Hydar.Cache.Data.Storage;
using Dargon.Hydar.Common;
using ItzWarty.Collections;
using Nito.AsyncEx;

namespace Dargon.Hydar.Cache.Data.Entries {
   public class CacheEntryContext<TKey, TValue> {
      private readonly IConcurrentQueue<ExecutableEntryOperation<TKey, TValue>> readOperationQueue = new ConcurrentQueue<ExecutableEntryOperation<TKey, TValue>>();
      private readonly IConcurrentQueue<ExecutableEntryOperation<TKey, TValue>> nonreadOperationQueue = new ConcurrentQueue<ExecutableEntryOperation<TKey, TValue>>();
      private readonly TKey key;
      private readonly AsyncSemaphore semaphore = new AsyncSemaphore(0);
      private readonly CacheStorageStrategy<TKey, TValue> cacheStorageStrategy;
      private TValue value;
      private bool exists;

      public CacheEntryContext(TKey key, CacheStorageStrategy<TKey, TValue> cacheStorageStrategy) {
         this.key = key;
         this.cacheStorageStrategy = cacheStorageStrategy;
      }

      public void Initialize() {
         Task.Factory.StartNew(async () => await OperationProcessingTaskStart());
      }

      private async Task OperationProcessingTaskStart() {
         while (true) {
            await semaphore.WaitAsync();
            semaphore.Release();

            if (!exists) {
               exists = cacheStorageStrategy.TryGet(key, out value);
            }

            ExecutableEntryOperation<TKey, TValue> operation;
            var entry = new EntryImpl<TKey, TValue>(key, value, exists);
            while (readOperationQueue.TryDequeue(out operation)) {
               await semaphore.WaitAsync();
               operation.Execute(entry);
            }

            bool isUpdated = false;
            while (nonreadOperationQueue.TryDequeue(out operation)) {
               await semaphore.WaitAsync();
               operation.Execute(entry);
               if (operation.Type == EntryOperationType.Put || 
                   operation.Type == EntryOperationType.Update ||
                   (operation.Type == EntryOperationType.ConditionalUpdate && entry.IsDirty) || 
                   operation.Type == EntryOperationType.Delete) {
                  // Set isUpdated so we update the cache context.
                  isUpdated = true;
                  
                  // Update exists if dirty and not deleted, reset dirty.
                  if (entry.IsDirty) {
                     entry.Exists = !entry.IsDeleted;
                     entry.IsDirty = false;
                  }

                  // Default value if deleted and unset deleted for next operation.
                  if (entry.IsDeleted) {
                     entry.Value = default(TValue);
                     entry.Exists = false;
                     entry.IsDeleted = false;
                  }
               }
            }

            if (isUpdated) {
               this.value = entry.Value;
               this.exists = entry.Exists;
               cacheStorageStrategy.Updated(entry);
            }
         }
      }

      public void EnqueueOperation(ExecutableEntryOperation<TKey, TValue> operation) {
         if (operation.Type == EntryOperationType.Read) {
            readOperationQueue.Enqueue(operation);
         } else {
            nonreadOperationQueue.Enqueue(operation);
         }
         semaphore.Release();
      }
   }
}