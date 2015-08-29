using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Common {
   public class HydarCommonApiPofContext : PofContext {
      private const int kBasePofId = 2200;

      public HydarCommonApiPofContext() {
         RegisterPortableObjectType(kBasePofId + 0, typeof(Int32IncrementProcessor<>));
      }
   }
}
