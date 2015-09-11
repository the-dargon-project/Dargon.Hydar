using Dargon.Platform.Accounts.Domain;
using Dargon.PortableObjects;

namespace Dargon.Platform.Accounts.Hydar {
   public class AccountsApiDomainPofContext : PofContext {
      private const int kBasePofId = 100000;

      public AccountsApiDomainPofContext() {
         RegisterPortableObjectType(kBasePofId + 0, typeof(Account));
         RegisterPortableObjectType(kBasePofId + 1, typeof(UsernameTakenException));
      }
   }
}
