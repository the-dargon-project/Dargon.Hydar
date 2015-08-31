using System.Threading.Tasks;
using Dargon.Hydar.Common;
using Dargon.PortableObjects;
using Nito.AsyncEx;

namespace Dargon.Hydar.Cache.Data {
   public abstract class EntryOperationBase<TKey, TValue, TResult> : EntryOperation<TKey, TValue, TResult> {
      private readonly AsyncManualResetEvent completionLatch = new AsyncManualResetEvent();
      private EntryOperationType type;
      private TKey key;
      private TResult internalResult;

      public EntryOperationBase(TKey key, EntryOperationType type) {
         this.key = key;
         this.type = type;
      }

      public TKey Key => key;
      public EntryOperationType Type => type;

      internal void __SetType(EntryOperationType newType) => type = newType;

      public async Task<TResult> GetResultAsync() {
         await completionLatch.WaitAsync();
         return internalResult;
      }

      void ExecutableEntryOperation<TKey, TValue>.Execute(Entry<TKey, TValue> entry) {
         SetResult(ExecuteInternal(entry));
      }

      public void SetResult(TResult result) {
         internalResult = result;
         completionLatch.Set();
      }

      protected abstract TResult ExecuteInternal(Entry<TKey, TValue> entry);

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
}