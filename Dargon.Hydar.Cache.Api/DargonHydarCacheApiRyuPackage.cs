using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Ryu;

namespace Dargon.Hydar.Cache {
   public class DargonHydarCacheApiRyuPackage : RyuPackageV1 {
      public DargonHydarCacheApiRyuPackage() {
         Singleton<CacheInitializerFacade, CacheInitializerFacadeImpl>();
      }
   }
}
