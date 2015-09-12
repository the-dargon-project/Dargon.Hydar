using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Platform.Feedback;
using Dargon.Ryu;
using Dargon.Platform.Webend;
using ItzWarty;
using Nancy;

namespace Dargon.Platform.Accounts.WebApi {
   public class FeedbackWebApiRyuPackage : RyuPackageV1 {
      public FeedbackWebApiRyuPackage() {
         this.WebApiModule<FeedbackWebApiModule>();
      }
   }

   public class FeedbackWebApiModule : WebApiModuleV1 {
      private const int kMaximumCompressedLogArchiveSize = 32 * 1024;
      private const int kMaximumDecompressedLogArchiveSize = 256 * 1024;

      private readonly AccountService accountService;
      private readonly ClientLogImportingService clientLogImportingService;

      public FeedbackWebApiModule(AccountService accountService, ClientLogImportingService clientLogImportingService) : base("/feedback") {
         this.clientLogImportingService = clientLogImportingService;
         this.accountService = accountService;
      }

      protected override void SetupRoutes() {
         Post["/client-logs", runAsync: true] = Proxy(UploadLogs);
      }

      private async Task<Response> UploadLogs(dynamic parameters, CancellationToken token) {
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

            var uploadedFileStream = new MemoryStream();
            file.Value.CopyTo(uploadedFileStream);

            ZipArchive zipArchive = null;
            if (Util.IsThrown<Exception>(() => zipArchive = new ZipArchive(uploadedFileStream, ZipArchiveMode.Read, true))) {
               return BadRequest("Failed to read zip archive.");
            }

            var decompressedSize = zipArchive.Entries.Sum(e => e.Length);
            if (decompressedSize > kMaximumDecompressedLogArchiveSize) {
               return BadRequest($"Invalid file - decompressed size was larger than {kMaximumDecompressedLogArchiveSize} bytes.");
            }

            Guid accountId;
            if (!accountService.TryValidateToken(accessToken, out accountId)) {
               return InvalidToken();
            }

            clientLogImportingService.ImportUserLogs(ClientDescriptor, uploadedFileStream.ToArray());
            return Success("Successfully uploaded logs.");
         }
      }
   }
}
