using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using ItzWarty;

namespace Dargon.Platform.Feedback {
   public interface ZipArchiveToMapConverter {
      IReadOnlyDictionary<string, string> Convert(ZipArchive archive);
   }

   public class ZipArchiveToMapConverterImpl : ZipArchiveToMapConverter {
      public IReadOnlyDictionary<string, string> Convert(ZipArchive archive) {
         var logContentsByFileName = new Dictionary<string, string>();

         foreach (var entry in archive.Entries) {
            var fileName = entry.Name;
            var fileNameLower = entry.Name.ToLower();
            var delimiterIndex = fileNameLower.LastIndexOf('.');

            string extensionlessFileName;
            if (delimiterIndex < 0) {
               extensionlessFileName = fileName;
            } else {
               extensionlessFileName = fileName.Substring(0, delimiterIndex);
               var extension = fileName.Substring(delimiterIndex + 1);
               if (!extension.Equals("log", StringComparison.OrdinalIgnoreCase)) {
                  continue;
               }
            }

            using (var entryDataStream = entry.Open())
            using (var reader = new BinaryReader(entryDataStream)) {
               var content = Encoding.UTF8.GetString(reader.ReadAllBytes());
               logContentsByFileName.Add(extensionlessFileName, content);
            }
         }

         return logContentsByFileName;
      }
   }
}