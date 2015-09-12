using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Platform.Webend {
   public class ApiResponse { }

   public class ResultResponse : ApiResponse {
      public bool Success => true;
      public ResponseCode ResponseCode { get; set; }
      public object Result { get; set; }
   }

   public class ErrorResponse : ApiResponse {
      public bool Success => false;
      public ResponseCode ResponseCode { get; set; }
      public object Error { get; set; }
   }
}
