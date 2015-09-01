using Dargon.Hydar.Common;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache.Data.Operations {
   public class EntryOperationProcess<TKey, TValue, TResult> : EntryOperationBase<TKey, TValue, TResult> {
      private EntryProcessor<TKey, TValue, TResult> processor;

      public EntryOperationProcess() : base(default(TKey), EntryOperationType.Update) { } 

      public EntryOperationProcess(TKey key, EntryProcessor<TKey, TValue, TResult> processor) : base(key, processor.Type) {
         this.processor = processor;
      }

      protected override TResult ExecuteInternal(Entry<TKey, TValue> entry) {
         return processor.Process(entry);
      }

      protected override void Serialize(IPofWriter writer, int slotOffset) {
         writer.WriteObject(slotOffset + 0, processor);
      }

      protected override void Deserialize(IPofReader reader, int slotOffset) {
         processor = reader.ReadObject<EntryProcessor<TKey, TValue, TResult>>(slotOffset + 0);
         __SetType(processor.Type);
      }
   }
}