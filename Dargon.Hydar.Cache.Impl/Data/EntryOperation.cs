using System.Threading.Tasks;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache.Data {
   public interface EntryOperation<TKey, TValue, TResult> : ExecutableEntryOperation<TKey, TValue>, IPortableObject {
      Task<TResult> GetResultAsync();
      void SetResult(TResult result);
   }
}