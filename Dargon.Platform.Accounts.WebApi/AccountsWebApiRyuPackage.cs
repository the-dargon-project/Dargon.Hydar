using System;
using Dargon.Ryu;
using Dargon.Platform.Webend;

namespace Dargon.Platform.Accounts.WebApi {
   public class AccountsWebApiRyuPackage : RyuPackageV1 {
      public AccountsWebApiRyuPackage() {
         this.WebApiModule<AccountsWebApiModule>();
      }
   }
}
