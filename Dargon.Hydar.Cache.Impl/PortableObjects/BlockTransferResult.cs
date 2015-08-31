using System.Collections.Generic;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache.PortableObjects {
   public class BlockTransferResult : IPortableObject {
      private IDictionary<uint, object> blocks;

      public BlockTransferResult() { }

      public BlockTransferResult(IDictionary<uint, object> blocks) {
         this.blocks = blocks;
      }

      public IReadOnlyDictionary<uint, object> Blocks => (IReadOnlyDictionary<uint, object>)blocks;

      public void Serialize(IPofWriter writer) {
         writer.WriteMap(0, blocks);
      }

      public void Deserialize(IPofReader reader) {
         blocks = reader.ReadMap<uint, object>(0);
      }
   }
}