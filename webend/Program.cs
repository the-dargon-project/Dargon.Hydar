using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace Dargon.Platform.FrontendApplicationBase {
   public class Options {
      [Option("PlatformServiceEndpoints", DefaultValue = "localhost:;")]
      public string PlatformServiceEndpoints { get; set; }
   }

   public static class Program {
      public static void Main(string[] args) {
         new WebendApplicationEgg().Start("http://localhost:1234");
         new AutoResetEvent(false).WaitOne();
      }
   }
}
