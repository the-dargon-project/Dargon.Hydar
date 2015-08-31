using System.Threading.Tasks;
using ItzWarty.Collections;

namespace Dargon.Hydar.Cache.Data {
   public class EntryBlockTable<TKey, TValue> {
      private readonly Keyspace keyspace;
      private readonly System.Collections.Generic.IReadOnlyList<Block<TKey, TValue>> blocks;
      private readonly IUniqueIdentificationSet haveBlocks = new UniqueIdentificationSet(false);

      public EntryBlockTable(Keyspace keyspace, System.Collections.Generic.IReadOnlyList<Block<TKey, TValue>> blocks) {
         this.keyspace = keyspace;
         this.blocks = blocks;
      }

      public void BlahBlahEmptyBlocks(IUniqueIdentificationSet set) {
         set.__Access(segments => {
            foreach (var segment in segments) {
               for (var blockId = segment.low; blockId <= segment.high; blockId++) {
                  blocks[(int)blockId].BlahBlahEmpty();
               }
               haveBlocks.GiveRange(segment.low, segment.high);
            }
         });
      }

      public bool BlahBlahHave(uint blockId) {
         return blocks[(int)blockId].IsUpToDate;
      }

      public IUniqueIdentificationSet IntersectNeed(IUniqueIdentificationSet need) {
         return haveBlocks.Intersect(need);
      }

      public Block<TKey, TValue> GetBlock(TKey key) {
         return blocks[keyspace.HashToBlockId(key.GetHashCode())];
      }

      public CacheEntryContext<TKey, TValue> GetEntry(TKey key) {
         var block = blocks[keyspace.HashToBlockId(key.GetHashCode())];
         return block.GetEntry(key);
      }

      public Task<TValue> GetAsync(TKey key) {
         return EnqueueAwaitableOperation(new EntryOperationGet<TKey, TValue>(key));
      }

      public Task<bool> PutAsync(TKey key, TValue value) {
         return EnqueueAwaitableOperation(new EntryOperationPut<TKey, TValue>(key, value));
      }

      public Task<TResult> EnqueueAwaitableOperation<TResult>(EntryOperation<TKey, TValue, TResult> entryOperation) {
         var entry = GetEntry(entryOperation.Key);
         entry.EnqueueOperation(entryOperation);
         return entryOperation.GetResultAsync();
      }
   }
}