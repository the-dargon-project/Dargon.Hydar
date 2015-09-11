using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Services;

namespace Dargon.Platform.Common {
   public interface PlatformNetworkingResources {
      ServiceClient LocalServiceClient { get; }
   }

   public class PlatformNetworkingResourcesImpl : PlatformNetworkingResources {
      public ServiceClient LocalServiceClient { get; set; }
   }
}
