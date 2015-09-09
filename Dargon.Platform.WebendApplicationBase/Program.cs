using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Platform.FrontendApplicationBase {
   public static class Program {
      public static void Main(string[] args) {
         new WebendApplicationEgg().Start("http://localhost:1234");
         new AutoResetEvent(false).WaitOne();
      }
   }
}
