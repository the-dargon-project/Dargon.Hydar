using Dargon.Hydar.Common;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache.Data.Operations {
   public interface ReadableEntryOperation<TKey, TValue> : IPortableObject {
      TKey Key { get; }
      EntryOperationType Type { get; }
   }
}