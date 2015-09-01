using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Hydar {
   public class PlatformSystemStateImpl : SystemState {
      public string Get(string key, string defaultValue) {
         return defaultValue;
      }

      public void Set(string key, string value) { }

      public bool GetBoolean(string key, bool defaultValue) {
         return defaultValue;
      }

      public void SetBoolean(string key, bool value) { }
   }
}
