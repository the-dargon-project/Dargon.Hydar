using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Hydar.Cache.Data.Storage;
using Dargon.Platform.Common;
using Dargon.Ryu;

namespace Dargon.Platform.Feedback {
   public class DargonPlatformFeedbackImplRyuPackage : RyuPackageV1 {
      public DargonPlatformFeedbackImplRyuPackage() {
         Singleton<Caches>();
         Singleton<CacheStore<Guid, ClientLog>>(ryu => ryu.Get<Caches>().ClientLogCacheStore);
         Singleton<ZipArchiveToMapConverter, ZipArchiveToMapConverterImpl>();
         this.LocalCorePlatformService<ClientLogImportingService, ClientLogImportingServiceImpl>();
      }
   }
}
