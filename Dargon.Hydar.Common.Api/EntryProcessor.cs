using Dargon.PortableObjects;

namespace Dargon.Hydar.Common {
   public interface EntryProcessor<TKey, TValue, TResult> : IPortableObject {
      EntryOperationType Type { get; }
      TResult Process(Entry<TKey, TValue> entry);
   }

   public class Int32IncrementProcessor<TKey> : EntryProcessor<TKey, int, int> {
      public EntryOperationType Type => EntryOperationType.Update;

      public int Process(Entry<TKey, int> entry) {
         var currentValue = entry.Value;
         var nextValue = currentValue + 1;
         entry.Value = nextValue;
         return nextValue;
      }

      public void Serialize(IPofWriter writer) { }
      public void Deserialize(IPofReader reader) { }
   }
}