using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Platform.Accounts.Hydar;
using Dargon.Ryu;

namespace Dargon.Platform.Accounts {
   public class DargonPlatformAccountsApiRyuPackage : RyuPackageV1 {
      public DargonPlatformAccountsApiRyuPackage() {
         PofContext<AccountsApiDomainPofContext>();
      }
   }
}
