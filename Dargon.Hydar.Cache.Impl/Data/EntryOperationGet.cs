using Dargon.Hydar.Common;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache.Data {
   public class EntryOperationGet<TKey, TValue> : EntryOperationBase<TKey, TValue, TValue> {
      public EntryOperationGet() : this(default(TKey)) { }

      public EntryOperationGet(TKey key) : base(key, EntryOperationType.Read) {}

      protected override TValue ExecuteInternal(Entry<TKey, TValue> entry) {
         return entry.Exists ? entry.Value : default(TValue);
      }

      protected override void Serialize(IPofWriter writer, int slotOffset) { }
      protected override void Deserialize(IPofReader reader, int slotOffset) { }
   }
}