using ItzWarty.Collections;
using SCG = System.Collections.Generic;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
      public class Block {
         public Block(int id) {
            Id = id;
         }

         public int Id { get; set; }
         public bool IsUpToDate { get; set; }

         public void BlahBlahEmpty() {
            IsUpToDate = true;
         }

         public void BlahBlahStale() {
            IsUpToDate = false;
         }
      }

      public class BlockTable {
         private readonly Keyspace keyspace;
         private readonly SCG.IReadOnlyList<Block> blocks;
         private IUniqueIdentificationSet haveBlocks = new UniqueIdentificationSet(false);

         public BlockTable(Keyspace keyspace, SCG.IReadOnlyList<Block> blocks) {
            this.keyspace = keyspace;
            this.blocks = blocks;
         }

         public void BlahBlahEmptyBlocks(IUniqueIdentificationSet set) {
            haveBlocks = haveBlocks.Merge(set);
            set.__Access(segments => {
               foreach (var segment in segments) {
                  for (var blockId = segment.low; blockId <= segment.high; blockId++) {
                     blocks[(int)blockId].BlahBlahEmpty();
                  }
               }
            });
         }

         public bool BlahBlahHave(uint blockId) {
            return blocks[(int)blockId].IsUpToDate;
         }

         public IUniqueIdentificationSet IntersectNeed(IUniqueIdentificationSet need) {
            return haveBlocks.Intersect(need);
         }
      }
   }
}
