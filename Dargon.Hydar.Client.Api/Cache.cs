using Dargon.Hydar.Common;

namespace Dargon.Hydar.Client {
   public interface Cache<TKey, TValue> {
      TValue Get(TKey key);
      TKey Put(TKey key, TValue value);
      TResult Process<TResult>(TKey key, EntryProcessor<TKey, TValue, TResult> entryProcessor);
      TResult Process<TResult, TProcessor>(TKey key, params object[] args) where TProcessor : EntryProcessor<TKey, TValue, TResult>;

   }

   public interface Entry<TKey, TValue> {
      TKey Key { get; set; }
      TValue Value { get; set; }

      bool Exists { get; }
      bool IsDirty { get; set; }
   }

   public interface EntryProcessor<TKey, TValue, TResult> {
      EntryOperationType Type { get; }
      TResult Process(Entry<TKey, TValue> entry);
   }
}
