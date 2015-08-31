using System;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache.PortableObjects {
   public abstract class HydarCacheMessageBase : IPortableObject {
      private Guid cacheId;

      protected HydarCacheMessageBase(Guid cacheId) {
         this.cacheId = cacheId;
      }

      public Guid CacheId => cacheId;

      public void Serialize(IPofWriter writer) {
         writer.WriteGuid(0, cacheId);
         Serialize(writer, 1);
      }

      protected abstract void Serialize(IPofWriter writer, int baseSlot);

      public void Deserialize(IPofReader reader) {
         cacheId = reader.ReadGuid(0);
         Deserialize(reader, 1);
      }

      protected abstract void Deserialize(IPofReader reader, int baseSlot);
   }
}