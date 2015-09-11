namespace Dargon.Platform.Common {
   public interface PlatformConfiguration {
      int ServicePort { get; }
   }

   public class PlatformConfigurationImpl : PlatformConfiguration {
      public int ServicePort { get; set; }
   }
}
