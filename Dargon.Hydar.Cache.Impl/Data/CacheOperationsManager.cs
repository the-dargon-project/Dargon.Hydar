using System;
using System.Threading.Tasks;
using Dargon.Hydar.Cache.Services;
using Dargon.Hydar.Common;
using Dargon.Services;
using ItzWarty;
using Nito.AsyncEx;
using SCG = System.Collections.Generic;

namespace Dargon.Hydar.Cache.Data {
   public class CacheOperationsManager<TKey, TValue> {
      private readonly AsyncReaderWriterLock synchronization = new AsyncReaderWriterLock();
      private readonly AsyncManualResetEvent resumedLatch = new AsyncManualResetEvent(false);
      private readonly Keyspace keyspace;
      private readonly EntryBlockTable<TKey, TValue> entryBlockTable;
      private readonly RemoteServiceContainer<TKey, TValue> remoteServiceContainer;
      private int nodeRank;
      private int nodeCount;
      private bool isSuspended = true;
      private SCG.IReadOnlyList<Guid> peers;

      public CacheOperationsManager(Keyspace keyspace, EntryBlockTable<TKey, TValue> entryBlockTable, RemoteServiceContainer<TKey, TValue> remoteServiceContainer) {
         this.keyspace = keyspace;
         this.entryBlockTable = entryBlockTable;
         this.remoteServiceContainer = remoteServiceContainer;
      }

      public void SuspendOperations() {
         using (synchronization.WriterLock()) {
            isSuspended = true;
            resumedLatch.Reset();
         }
      }

      public void ResumeOperations(int nodeRank, int nodeCount, SCG.IReadOnlyList<Guid> peers) {
         using (synchronization.WriterLock()) {
            this.isSuspended = false;
            this.nodeRank = nodeRank;
            this.nodeCount = nodeCount;
            this.peers = peers;
            this.resumedLatch.Set();
         }
      }

      public async Task<TResult> EnqueueAndAwaitResults<TResult>(EntryOperation<TKey, TValue, TResult> operation) {
         while (true) {
            await resumedLatch.WaitAsync();
            using (var readerLock = await synchronization.ReaderLockAsync()) {
               if (!isSuspended) {
                  var blockId = (uint)keyspace.HashToBlockId(operation.Key.GetHashCode());
                  var partitionRanges = keyspace.GetNodePartitionRanges(nodeRank, nodeCount);
                  var maxPartitionIndexUpperExclusive = operation.Type == EntryOperationType.Read ? partitionRanges.Length : 1;
                  var blockPartitionIndex = Array.FindIndex(partitionRanges, interval => interval.Contains(blockId));
                  Console.WriteLine("Key: " + operation.Key + ", Block: " + blockId + ", Local: " + partitionRanges.Join(", "));
                  if (blockPartitionIndex != -1 && blockPartitionIndex < maxPartitionIndexUpperExclusive) {
                     // perform locally
                     return await entryBlockTable.EnqueueAwaitableOperation(operation);
                  } else {
                     // perform networked
                     var peerIndex = keyspace.GetPeerIndex((int)blockId, nodeCount, operation.Type == EntryOperationType.Read);
                     Console.WriteLine("PeerIndex: " + peerIndex);
                     Console.WriteLine("Peers: " + peers.Join(", "));
                     var peerCacheService = remoteServiceContainer.GetCacheService(peers[peerIndex]);
                     return await AsyncStatics.Async(() => peerCacheService.ExecuteProxiedOperation<TResult>(operation));
                  }
               }
            }
         }
      }
   }
}