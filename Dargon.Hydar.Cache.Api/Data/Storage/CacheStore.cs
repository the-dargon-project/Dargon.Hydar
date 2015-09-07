using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Hydar.Cache.Data.Storage {
   public interface CacheStore<TKey, TValue> {
      bool TryGet(TKey key, out TValue value);
      void Delete(TKey key);
      void Update(TKey key, TValue value);
   }
}
