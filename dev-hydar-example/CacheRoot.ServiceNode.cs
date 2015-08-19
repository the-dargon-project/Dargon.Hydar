using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Services;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
      public interface ClientCacheService {

      }

      public interface InterCacheService {
         IReadOnlyDictionary<uint, object> TransferBlocks(PartitionBlockInterval[] blockIntervals);
      }

      public interface CacheService : ClientCacheService, InterCacheService { }

      public class CacheServiceImpl : CacheService {
         public IReadOnlyDictionary<uint, object> TransferBlocks(PartitionBlockInterval[] blockIntervals) {
            var result = new Dictionary<uint, object>();
            foreach (var interval in blockIntervals) {
               for (var blockId = interval.StartBlockInclusive; blockId < interval.EndBlockExclusive; blockId++) {
                  result.Add(blockId, new object());
               }
            }
            return result;
         }
      }
   }
}
