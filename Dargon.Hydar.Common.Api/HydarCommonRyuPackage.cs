using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Ryu;

namespace Dargon.Hydar.Common {
   public class HydarCommonRyuPackage : RyuPackageV1 {
      public HydarCommonRyuPackage() {
         PofContext<HydarCommonApiPofContext>();
      }
   }
}
