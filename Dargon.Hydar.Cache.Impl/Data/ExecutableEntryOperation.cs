using Dargon.Hydar.Common;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache.Data {
   public interface ExecutableEntryOperation<TKey, TValue> : ReadableEntryOperation<TKey, TValue>, IPortableObject {
      void Execute(Entry<TKey, TValue> entry);
   }
}