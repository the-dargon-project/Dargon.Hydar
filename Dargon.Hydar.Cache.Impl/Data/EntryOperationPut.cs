using Dargon.Hydar.Common;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache.Data {
   public class EntryOperationPut<TKey, TValue> : EntryOperationBase<TKey, TValue, bool> {
      private TValue value;

      public EntryOperationPut() : this(default(TKey), default(TValue)) { }

      public EntryOperationPut(TKey key, TValue value) : base(key, EntryOperationType.Put) {
         this.value = value;
      }

      public TValue Value => value;

      protected override bool ExecuteInternal(Entry<TKey, TValue> entry) {
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
}