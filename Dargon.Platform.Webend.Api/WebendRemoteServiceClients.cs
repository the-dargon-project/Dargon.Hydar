using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Services;

namespace Dargon.Platform.Webend {
   public interface WebendRemoteServiceClients {
      IServiceClient CorePlatform { get; }
   }

   public class WebendRemoteServiceClientsImpl : WebendRemoteServiceClients {
      public IServiceClient CorePlatform { get; set; }
   }
}
