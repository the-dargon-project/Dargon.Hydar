namespace Dargon.Hydar.Cache {
   public interface HydarConfiguration {
      int ServicePort { get; }
      int CourierPort { get; }
   }

   public class HydarConfigurationImpl : HydarConfiguration {
      public int CourierPort { get; set; }
      public int ServicePort { get; set; }
   }
}
