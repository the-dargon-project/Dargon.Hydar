using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache.PortableObjects {
   public class EntryTryGetResult<TValue> : IPortableObject {
      private bool success;
      private TValue value;

      public EntryTryGetResult() { } 

      public EntryTryGetResult(bool success, TValue value) {
         this.success = success;
         this.value = value;
      }

      public bool Success => success;
      public TValue Value => value;

      public void Serialize(IPofWriter writer) {
         writer.WriteBoolean(0, success);
         writer.WriteObject(1, value);
      }

      public void Deserialize(IPofReader reader) {
         success = reader.ReadBoolean(0);
         value = reader.ReadObject<TValue>(1);
      }
   }
}
