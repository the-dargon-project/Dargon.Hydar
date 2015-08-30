using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Courier.Messaging;
using Dargon.Hydar.Utilities;
using Dargon.Ryu;

namespace Dargon.Hydar {
   public class HydarRyuPackage : RyuPackageV1 {
      public HydarRyuPackage() {
         Singleton<GuidHelper, GuidHelperImpl>();
         Singleton<ReceivedMessageFactory, ReceivedMessageFactoryImpl>();

         PofContext<HydarCachePofContext>();
      }
   }
}
