using Dargon.Hydar.Cache.Data.Storage;
using Dargon.Platform.Common;
using Dargon.Zilean;
using ItzWarty;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Dargon.Platform.Feedback {
   public class ClientLogImportingServiceImpl : ClientLogImportingService {
      private readonly ChronokeeperService chronokeeperService; 
      private readonly ZipArchiveToMapConverter zipArchiveToMapConverter;
      private readonly CacheStore<Guid, ClientLog> clientLogCacheStore;

      public ClientLogImportingServiceImpl(ChronokeeperService chronokeeperService, CacheStore<Guid, ClientLog> clientLogCacheStore, ZipArchiveToMapConverter zipArchiveToMapConverter) {
         this.chronokeeperService = chronokeeperService;
         this.clientLogCacheStore = clientLogCacheStore;
         this.zipArchiveToMapConverter = zipArchiveToMapConverter;
      }

      public void ImportUserLogs(ClientDescriptor clientDescriptor, byte[] zipArchiveContents) {
         using (var ms = new MemoryStream(zipArchiveContents))
         using (var zipArchive = new ZipArchive(ms, ZipArchiveMode.Read)) {
            var archiveContents = zipArchiveToMapConverter.Convert(zipArchive).ToList();

            var identifiers = chronokeeperService.GenerateSequentialGuids(archiveContents.Count + 1);
            var batchId = identifiers[0];
            var fileIds = identifiers.SubArray(1);
            
            for (var i = 0; i < archiveContents.Count; i++) {
               var kvp = archiveContents[i];
               var fileName = kvp.Key;
               var fileContents = kvp.Value;
               var fileId = fileIds[i];
               clientLogCacheStore.Insert(
                  fileId,
                  new ClientLog {
                     Id = fileId,
                     BatchId = batchId,
                     FileName = fileName,
                     Uploaded = DateTime.Now,
                     ClientId = clientDescriptor.Id,
                     ClientName = clientDescriptor.Name ?? "?",
                     ClientVersion = clientDescriptor.Version ?? "?",
                     Contents = fileContents,
                  });
            }
         }
      }
   }
}
