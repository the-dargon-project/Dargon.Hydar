using CommandLine;

namespace Dargon.Hydar {
   public class CorePlatformOptions {
      [Option('p', "ServicePort", DefaultValue = 31337, HelpText = "Core Platform Dargon.Services port (tcp).")]
      public int ServicePort { get; set; }

      [Option('m', "ManagementPort", DefaultValue = 31000, HelpText = "Platform Dargon.Management port (tcp).")]
      public int ManagementPort { get; set; }

      [Option('c', "ConnectionString", DefaultValue = "Server=127.0.0.1;Port=5432;Database=dargon;User Id=dargon_development;Password=dargon;")]
      public string ConnectionString { get; set; }

      [Option("HydarServicePort", DefaultValue = 32000, HelpText = "Hydar cache Dargon.Services port (tcp).")]
      public int HydarServicePort { get; set; }

      [Option("HydarCourierPort", DefaultValue = 31001, HelpText = "Hydar Dargon.Courier port (udp).")]
      public int HydarCourierPort { get; set; }
   }
}