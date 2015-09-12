using Dargon.Platform.Webend;
using Dargon.Ryu;
using FluentValidation;
using ItzWarty;
using Nancy;
using System;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Dargon.Services.AsyncStatics;

namespace Dargon.Platform.Accounts.WebApi {
   public class AccountsWebApiModule : WebApiModuleV1 {
      private readonly AccountService accountService;

      public AccountsWebApiModule(AccountService accountService) {
         this.accountService = accountService;
      }

      protected override void SetupRoutes() {
         Post["/login", runAsync: true] = Proxy<AuthenticationRequest>(LogIn);
         Post["/accounts", runAsync: true] = Proxy<CreateAccountRequest>(CreateAccount);
      }

      private async Task<Response> LogIn(dynamic parameters, CancellationToken token, AuthenticationRequest x) {
         string username = x.Username;
         string saltedPassword = x.Password;
         Guid accountId = Guid.Empty;
         string accessToken = null;
         var authenticationSuccessful = await Async(() => accountService.TryAuthenticate(username, saltedPassword, out accountId, out accessToken));
         if (!authenticationSuccessful) {
            return Failure("Authentication failed.");
         } else {
            return Success(new { AccountId = accountId, AccessToken = accessToken });
         }
      }

      private async Task<Response> CreateAccount(dynamic parameters, CancellationToken token, CreateAccountRequest x) {
         try {
            var accountId = accountService.CreateAccount(x.Username, x.Password);
            return Success(new { AccountId = accountId });
         } catch (UsernameTakenException ex) {
            return Failure(ex.Message);
         }
      }

      public class AccountsWebApiRyuPackage : RyuPackageV1 {
         public AccountsWebApiRyuPackage() {
            Singleton<CreateAccountRequestValidator>(RyuTypeFlags.Required);
         }
      }

      public class AuthenticationRequest {
         public string Username { get; set; }
         public string Password { get; set; }
      }

      public class AuthenticationRequestValidator : AbstractValidator<AuthenticationRequest> {
         public AuthenticationRequestValidator() {
            RuleFor(x => x.Username).Length(4, 32);
            RuleFor(x => x.Password).NotEmpty();
         }
      }

      public class CreateAccountRequest {
         public string Username { get; set; }
         public string Password { get; set; }
      }

      public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest> {
         public CreateAccountRequestValidator() {
            RuleFor(x => x.Username).Length(4, 32);
            RuleFor(x => x.Password).NotEmpty();
         }
      }
   }
}
