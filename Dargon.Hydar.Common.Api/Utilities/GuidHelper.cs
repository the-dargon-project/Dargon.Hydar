using System;
using System.Security.Cryptography;
using System.Text;

namespace Dargon.Hydar.Common.Utilities {
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
