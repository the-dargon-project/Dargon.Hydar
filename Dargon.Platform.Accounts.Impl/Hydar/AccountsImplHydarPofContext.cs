using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Platform.Accounts.Domain;
using Dargon.Platform.Accounts.Hydar.Processors;
using Dargon.PortableObjects;

namespace Dargon.Platform.Accounts.Hydar {
   public class AccountsImplHydarPofContext : PofContext {
      private const int kBasePofId = 100500;

      public AccountsImplHydarPofContext() {
         RegisterPortableObjectType(kBasePofId + 0, typeof(AccountAuthenticationProcessor));
      }
   }
}
