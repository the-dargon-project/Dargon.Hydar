using Dargon.Hydar.Cache.Data.Operations;
using Dargon.Hydar.Cache.PortableObjects;

namespace Dargon.Hydar.Cache.Services {
   public interface InterCacheService<TKey, TValue> {
      BlockTransferResult TransferBlocks(PartitionBlockInterval[] blockIntervals);
      TResult ExecuteProxiedOperation<TResult>(EntryOperation<TKey, TValue, TResult> operation);
   }
}