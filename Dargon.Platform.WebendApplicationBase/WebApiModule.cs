using System;
using System.Reflection;
using Dargon.Ryu;
using Nancy;
using NLog;

namespace Dargon.Platform.FrontendApplicationBase {
   public abstract class WebApiModule : NancyModule {
      private static readonly Logger logger = LogManager.GetCurrentClassLogger();

      protected WebApiModule(string pathOffset = null) : base(BuildPath(WebendGlobals.kApiBasePath, pathOffset)) {
         InitializeUserFields();
      }

      private void InitializeUserFields() {
         var type = this.GetType();
         var privateFields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
         foreach (var field in privateFields) {
            var fieldType = field.GetType();
            if (!fieldType.IsValueType) {
               try {
                  var value = ryu.Get(fieldType);
                  field.SetValue(this, value);
               } catch (Exception e) {
                  logger.Error($"Initializing field `{field.Name}` of type `{fieldType.FullName}` in `{type.FullName}` threw", e);
               }
            }
         }
      }

      // Statics
      private static RyuContainer ryu;

      public static void SetRyuContainer(RyuContainer ryuContainer) {
         ryu = ryuContainer;
      }

      private static string BuildPath(string apiBasePath, string pathOffset) {
         if (string.IsNullOrWhiteSpace(pathOffset)) {
            return apiBasePath;
         } else {
            return apiBasePath + "/" + pathOffset;
         }
      }
   }
}