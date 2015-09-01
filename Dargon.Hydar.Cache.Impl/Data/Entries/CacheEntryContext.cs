using System.Threading.Tasks;
using Dargon.Hydar.Cache.Data.Operations;
using Dargon.Hydar.Common;
using ItzWarty.Collections;
using Nito.AsyncEx;

namespace Dargon.Hydar.Cache.Data.Entries {
   public class CacheEntryContext<TKey, TValue> {
      private readonly IConcurrentQueue<ExecutableEntryOperation<TKey, TValue>> readOperationQueue = new ConcurrentQueue<ExecutableEntryOperation<TKey, TValue>>();
      private readonly IConcurrentQueue<ExecutableEntryOperation<TKey, TValue>> nonreadOperationQueue = new ConcurrentQueue<ExecutableEntryOperation<TKey, TValue>>();
      private readonly TKey key;
      private readonly AsyncSemaphore semaphore = new AsyncSemaphore(0);
      private TValue value;
      private bool exists;

      public CacheEntryContext(TKey key) {
         this.key = key;
      }

      public void Initialize() {
         Task.Factory.StartNew(async () => await OperationProcessingTaskStart());
      }

      private async Task OperationProcessingTaskStart() {
         while (true) {
            await semaphore.WaitAsync();
            semaphore.Release();

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
                   (operation.Type == EntryOperationType.ConditionalUpdate && entry.IsDirty)) {
                  // Set isUpdated so we update the cache context.
                  isUpdated = true;

                  // Unset Dirty flag for next execute operation.
                  entry.IsDirty = false;
               }
            }

            if (isUpdated) {
               this.value = entry.Value;
               this.exists = true;
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