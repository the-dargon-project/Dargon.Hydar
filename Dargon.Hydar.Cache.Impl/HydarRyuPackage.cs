using System;
using Dargon.Courier.Messaging;
using Dargon.Hydar.Common.Utilities;
using Dargon.Ryu;

namespace Dargon.Hydar.Cache {
   public class HydarRyuPackage : RyuPackageV1 {
      public HydarRyuPackage() {
         Singleton<CacheFactory, CacheFactoryImpl>();
         Singleton<CacheDispatcher, CacheDispatcherImpl>();

         PofContext<HydarCachePofContext>();
      }
   }
}
