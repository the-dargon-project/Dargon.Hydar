using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Platform.Accounts {
   public interface AccountLookupService {
      bool TryGetAccountIdByUsername(string name, out Guid accountId);
   }
}
