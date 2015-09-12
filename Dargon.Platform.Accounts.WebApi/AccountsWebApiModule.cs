using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Platform.Webend;
using Dargon.Ryu;
using FluentValidation;
using ItzWarty;
using Nancy;
using Nancy.Json;
using Nancy.ModelBinding;
using Nancy.Validation;
using static Dargon.Services.AsyncStatics;

namespace Dargon.Platform.Accounts.WebApi {
   public class AccountsWebApiModule : WebApiModuleV1 {
      private const int kMaximumCompressedLogArchiveSize = 32 * 1024;
      private const int kMaximumDecompressedLogArchiveSize = 256 * 1024;

      private readonly AccountService accountService;

      public AccountsWebApiModule(AccountService accountService) {
         this.accountService = accountService;
      }

      protected override void SetupRoutes() {
         Post["/login", runAsync: true] = Proxy<AuthenticationRequest>(LogIn);
         Post["/accounts", runAsync: true] = Proxy<CreateAccountRequest>(CreateAccount);
         Post["/accounts/{accountId:guid}/logs", runAsync: true] = Proxy(UploadLogs);
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

      private async Task<Response> UploadLogs(dynamic parameters, CancellationToken token) {
         var accountId = (Guid)parameters.accountId;

         string accessToken;
         if (!TryGetAccessToken(out accessToken)) {
            return Unauthorized("No access token specified.");
         }

         var files = Request.Files.ToList();
         if (files.None()) {
            return BadRequest("No file specified.");
         } else if (files.Count >= 2) {
            return BadRequest("Multiple files specified.");
         } else {
            var file = files.First();
            
            if (file.ContentType != "application/zip" && file.ContentType != "application/octet-stream" && file.ContentType != "application/x-zip-compressed") {
               return BadRequest("Invalid file - must specifiy zip archive.");
            } else if (file.Value.Length > kMaximumCompressedLogArchiveSize) {
               return BadRequest($"Invalid file - compressed size was larger than {kMaximumCompressedLogArchiveSize} bytes.");
            }

            ZipArchive zipArchive = null;
            if (Util.IsThrown<Exception>(() => zipArchive = new ZipArchive(file.Value, ZipArchiveMode.Read, true))) {
               return BadRequest("Failed to read zip archive.");
            }

            var decompressedSize = zipArchive.Entries.Sum(e => e.Length);
            if (decompressedSize > kMaximumDecompressedLogArchiveSize) {
               return BadRequest($"Invalid file - decompressed size was larger than {kMaximumDecompressedLogArchiveSize} bytes.");
            }

            Guid tokenAccountId;
            if (!accountService.TryValidateToken(accessToken, out tokenAccountId)) {
               return InvalidToken();
            } else if (!tokenAccountId.Equals(accountId)) {
               return Unauthorized("The provided access token is for another account.");
            }

            return Success("Successfully uploaded logs.");
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
