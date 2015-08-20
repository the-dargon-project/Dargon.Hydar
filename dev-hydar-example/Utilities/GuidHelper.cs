using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ItzWarty;

namespace Dargon.Hydar.Utilities {
   public interface GuidHelper {
      Guid ComputeMd5(string input);
   }

   public class GuidHelperImpl : GuidHelper {
      public Guid ComputeMd5(string input) {
         using (var md5 = MD5.Create()) {
            return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(input)));
         }
      }
   }
}
