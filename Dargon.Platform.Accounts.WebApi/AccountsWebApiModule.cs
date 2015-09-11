using System;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Platform.Webend;
using static Dargon.Services.AsyncStatics;

namespace Dargon.Platform.Accounts.WebApi {
   public class AccountsWebApiModule : WebApiModuleV1 {
      private readonly AccountService accountService;

      public AccountsWebApiModule(AccountService accountService) {
         this.accountService = accountService;
      }

      protected override void SetupRoutes() {
         Get["/authenticate", runAsync: true] = ProxyAsJson(Authenticate);
      }

      private async Task<dynamic> Authenticate(dynamic parameters, CancellationToken token) {
         string username = "warty";
         string saltedPassword = "test";
         Guid accountId = Guid.Empty, accessToken = Guid.Empty;
//         var authenticationResult = await Async(() => accountService.TryAuthenticate(username, saltedPassword, out accountId, out accessToken));
         var authenticationResult = accountService.TryAuthenticate(username, saltedPassword, out accountId, out accessToken);
         return new { Success = authenticationResult, AccountId = accountId, AccessToken = accessToken };
      }
   }
}
