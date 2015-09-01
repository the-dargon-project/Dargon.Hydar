using Dargon.Hydar.Cache.PortableObjects;
using Dargon.Hydar.Common;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache.Data.Operations {
   public class EntryOperationTryGet<TKey, TValue> : EntryOperationBase<TKey, TValue, EntryTryGetResult<TValue>> {
      public EntryOperationTryGet() : this(default(TKey)) { }

      public EntryOperationTryGet(TKey key) : base(key, EntryOperationType.Read) {}

      protected override EntryTryGetResult<TValue> ExecuteInternal(Entry<TKey, TValue> entry) {
         return new EntryTryGetResult<TValue>(entry.Exists, entry.Value);
      }

      protected override void Serialize(IPofWriter writer, int slotOffset) { }
      protected override void Deserialize(IPofReader reader, int slotOffset) { }
   }
}