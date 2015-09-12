using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.PortableObjects;

namespace Dargon.Platform.Common {
   public class DargonPlatformCommonApiPofContext : PofContext {
      private const int kBasePofId = 101000;

      public DargonPlatformCommonApiPofContext() {
         RegisterPortableObjectType(kBasePofId + 0, typeof(ClientDescriptor));
      }
   }
}
