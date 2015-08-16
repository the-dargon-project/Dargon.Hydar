using System;
using System.Security.Cryptography.X509Certificates;
using Dargon.PortableObjects;
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
