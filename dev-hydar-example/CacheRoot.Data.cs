using Dargon.PortableObjects;
using ItzWarty.Collections;
using System;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Courier.Peering;
using ItzWarty;
using Nito.AsyncEx;
using SCG = System.Collections.Generic;
using static Dargon.Services.AsyncStatics;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
      public class CacheEntryContext {
         private readonly IConcurrentQueue<ExecutableEntryOperation> readOperationQueue = new ConcurrentQueue<ExecutableEntryOperation>();
         private readonly IConcurrentQueue<ExecutableEntryOperation> nonreadOperationQueue = new ConcurrentQueue<ExecutableEntryOperation>();
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

               ExecutableEntryOperation operation;
               Entry entry = new Entry(key, value, exists);
               while (readOperationQueue.TryDequeue(out operation)) {
                  await semaphore.WaitAsync();
                  operation.Execute(entry);
               }

               bool isUpdated = false;
               while (nonreadOperationQueue.TryDequeue(out operation)) {
                  await semaphore.WaitAsync();
                  operation.Execute(entry);
                  if (entry.IsDirty) {
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

         public void EnqueueOperation(ExecutableEntryOperation operation) {
            if (operation.Type == EntryOperationType.Read) {
               readOperationQueue.Enqueue(operation);
            } else {
               nonreadOperationQueue.Enqueue(operation);
            }
            semaphore.Release();
         }
      }

      public class Entry {
         private readonly TKey key;
         private readonly bool exists;
         private TValue value;
         private bool isDirty;

         public Entry(TKey key, TValue value, bool exists) {
            this.key = key;
            this.value = value;
            this.exists = exists;
         }

         public TKey Key => key;
         public bool Exists => exists;
         public TValue Value {  get { return this.value; } set { this.value = value; } }
         public bool IsDirty { get { return this.isDirty; } set { this.isDirty = true; } }
      }

      public interface ReadableEntryOperation : IPortableObject {
         TKey Key { get; }
         EntryOperationType Type { get; }
      }

      public interface ExecutableEntryOperation : ReadableEntryOperation, IPortableObject {
         void Execute(Entry entry);
      }

      public interface EntryOperation<TResult> : ExecutableEntryOperation, IPortableObject {
         Task<TResult> GetResultAsync();
         void SetResult(TResult result);
      }

      public abstract class EntryOperationBase<TResult> : EntryOperation<TResult> {
         private readonly AsyncManualResetEvent completionLatch = new AsyncManualResetEvent();
         private readonly EntryOperationType type;
         private TKey key;
         private TResult internalResult;

         public EntryOperationBase(TKey key, EntryOperationType type) {
            this.key = key;
            this.type = type;
         }

         public TKey Key => key;
         public EntryOperationType Type => type;

         public async Task<TResult> GetResultAsync() {
            await completionLatch.WaitAsync();
            return internalResult;
         }

         void ExecutableEntryOperation.Execute(Entry entry) {
            SetResult(ExecuteInternal(entry));
         }

         public void SetResult(TResult result) {
            internalResult = result;
            completionLatch.Set();
         }

         protected abstract TResult ExecuteInternal(Entry entry);

         public void Serialize(IPofWriter writer) {
            writer.WriteObject(0, key);
            Serialize(writer, 1);
         }

         protected abstract void Serialize(IPofWriter writer, int slotOffset);

         public void Deserialize(IPofReader reader) {
            key = reader.ReadObject<TKey>(0);
            Deserialize(reader, 1);
         }

         protected abstract void Deserialize(IPofReader reader, int slotOffset);
      }

      public class EntryOperationPut : EntryOperationBase<bool> {
         private TValue value;

         public EntryOperationPut() : this(default(TKey), default(TValue)) { }

         public EntryOperationPut(TKey key, TValue value) : base(key, EntryOperationType.Put) {
            this.value = value;
         }

         public TValue Value => value;

         protected override bool ExecuteInternal(Entry entry) {
            entry.Value = value;
            entry.IsDirty = true;
            return entry.Exists;
         }

         protected override void Serialize(IPofWriter writer, int slotOffset) {
            writer.WriteObject(slotOffset + 0, value);
         }

         protected override void Deserialize(IPofReader reader, int slotOffset) {
            value = reader.ReadObject<TValue>(slotOffset + 0);
         }
      }

      public class EntryOperationGet : EntryOperationBase<TValue> {
         public EntryOperationGet() : this(default(TKey)) { }

         public EntryOperationGet(TKey key) : base(key, EntryOperationType.Read) {}

         protected override TValue ExecuteInternal(Entry entry) {
            return entry.Exists ? entry.Value : default(TValue);
         }

         protected override void Serialize(IPofWriter writer, int slotOffset) { }
         protected override void Deserialize(IPofReader reader, int slotOffset) { }
      }

      public enum EntryOperationType {
         Read = 0x01,
         Put = 0x02,
         Update = 0x04,
         ConditionalUpdate = 0x08
      }

      public class Block {
         private readonly IConcurrentDictionary<TKey, CacheEntryContext> entryContextsByKey = new ConcurrentDictionary<TKey, CacheEntryContext>();

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

         public CacheEntryContext GetEntry(TKey key) {
            return entryContextsByKey.GetOrAdd(
               key,
               new CacheEntryContext(key).With(x => x.Initialize())
            );
         }
      }

      public class EntryBlockTable {
         private readonly Keyspace keyspace;
         private readonly SCG.IReadOnlyList<Block> blocks;
         private readonly IUniqueIdentificationSet haveBlocks = new UniqueIdentificationSet(false);

         public EntryBlockTable(Keyspace keyspace, SCG.IReadOnlyList<Block> blocks) {
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

         public Block GetBlock(TKey key) {
            return blocks[keyspace.HashToBlockId(key.GetHashCode())];
         }

         public CacheEntryContext GetEntry(TKey key) {
            var block = blocks[keyspace.HashToBlockId(key.GetHashCode())];
            return block.GetEntry(key);
         }

         public Task<TValue> GetAsync(TKey key) {
            return EnqueueAwaitableOperation(new EntryOperationGet(key));
         }

         public Task<bool> PutAsync(TKey key, TValue value) {
            return EnqueueAwaitableOperation(new EntryOperationPut(key, value));
         }

         public Task<TResult> EnqueueAwaitableOperation<TResult>(EntryOperation<TResult> entryOperation) {
            var entry = GetEntry(entryOperation.Key);
            entry.EnqueueOperation(entryOperation);
            return entryOperation.GetResultAsync();
         }
      }

      public class CacheOperationsManager {
         private readonly AsyncReaderWriterLock synchronization = new AsyncReaderWriterLock();
         private readonly AsyncManualResetEvent resumedLatch = new AsyncManualResetEvent(false);
         private readonly Keyspace keyspace;
         private readonly EntryBlockTable entryBlockTable;
         private readonly RemoteServiceContainer remoteServiceContainer;
         private int nodeRank;
         private int nodeCount;
         private bool isSuspended = true;
         private SCG.IReadOnlyList<Guid> peers;

         public CacheOperationsManager(Keyspace keyspace, EntryBlockTable entryBlockTable, RemoteServiceContainer remoteServiceContainer) {
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

         public async Task<TResult> EnqueueAndAwaitResults<TResult>(EntryOperation<TResult> operation) {
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
                        return await Async(() => peerCacheService.ExecuteProxiedOperation<TResult>(operation));
                     }
                  }
               }
            }
         }
      }

      public class TransactionLog {
         private readonly SCG.List<Commit> pending = new SCG.List<Commit>();
         private readonly SCG.List<Commit> committed = new SCG.List<Commit>();
         private readonly SCG.List<Commit> aborted = new SCG.List<Commit>(); 
      }

      public class Commit {
         public TKey Key { get; set; }
      }

      public interface Transaction {
         string Namespace { get; set; }
         Guid Identifier { get; set; }
         Guid Origin { get; set; }
         DateTime LastUpdated { get; set; }
         TimeSpan InitialTimeToLive { get; set; }
         TimeSpan CurrentTimeToLive { get; set; }

         void HandleNamespaceTransaction(Transaction transaction);
         void Reject();
      }

      public abstract class TransactionBase : Transaction, IPortableObject {
         public string Namespace { get; set; }
         public Guid Identifier { get; set; }
         public Guid Origin { get; set; }
         public DateTime LastUpdated { get; set; }
         public TimeSpan InitialTimeToLive { get; set; }
         public TimeSpan CurrentTimeToLive { get; set; }
         public TransactionManager TransactionManager { get; set; }
         public TransactionState TransactionState { get; set; }

         public abstract void HandleNamespaceTransaction(Transaction transaction);
         public abstract void HandleExpiry();

         public void Reject() { throw new NotImplementedException(); }
         public void MarkUpdatedAndResetTtl() {
            CurrentTimeToLive = InitialTimeToLive;
            LastUpdated = DateTime.Now;
         }

         public void Serialize(IPofWriter writer) {
            SerializeInternal(writer, 0);
         }

         public void Deserialize(IPofReader reader) {
            DeserializeInternal(reader, 0);
         }

         public abstract void SerializeInternal(IPofWriter writer, int slotOffset);

         public abstract void DeserializeInternal(IPofReader reader, int slotOffset);
      }

      public class TransactionMessenger {

      }

      public class TransactionManager {
         public void AbortAndYieldTo(Transaction transaction) { }
      }

      public class ElectionTransaction : TransactionBase {
         private readonly HashSet<Guid> acknowledged = new HashSet<Guid>();

         public override void HandleNamespaceTransaction(Transaction transaction) {
            var et = (ElectionTransaction)transaction;
            if (et.Identifier.CompareTo(this.Identifier) > 0) {
               TransactionManager.AbortAndYieldTo(transaction);
            } else {
               transaction.Reject();
               if (acknowledged.Add(transaction.Origin)) {
                  MarkUpdatedAndResetTtl();
               }
            }
         }

         public override void HandleExpiry() {

         }

         public override void SerializeInternal(IPofWriter writer, int slotOffset) {
            
         }

         public override void DeserializeInternal(IPofReader reader, int slotOffset) {
         }
      }

      public class RepartitionTransaction : TransactionBase {
         public override void HandleNamespaceTransaction(Transaction transaction) {

         }

         public override void HandleExpiry() {

         }

         public override void SerializeInternal(IPofWriter writer, int slotOffset) {

         }

         public override void DeserializeInternal(IPofReader reader, int slotOffset) {
         }
      }
   }

   [Flags]
   public enum TransactionState {
      None = 0,
      Proposed = 0x01,
      Accepted = 0x02,
      Rejected = 0x04,
      Aborted = 0x08,
      Committed = 0x10
   }
}
