using Dargon.Ryu;

namespace Dargon.Platform.Webend {
   public static class WebendRyuPackageV1Extensions {
      public static void WebApiModule<TWebApiModule>(this RyuPackageV1 ryuPackage) {
         ryuPackage.Singleton<TWebApiModule>(RyuTypeFlags.Required);
      }
   }
}
