using Dargon.Hydar.Common;

namespace Dargon.Hydar.Client {
   public interface Cache<TKey, TValue> {
      TResult Process<TResult>(TKey key, EntryProcessor<TKey, TValue, TResult> entryProcessor);
      TResult Process<TResult, TProcessor>(TKey key, params object[] args) where TProcessor : EntryProcessor<TKey, TValue, TResult>;

      bool TryGetValue(TKey key, out TValue value);
      bool ContainsKey(TKey key);

      TValue this[TKey key] { get; set; }
   }

   public static class CacheExtensions {
      public static TValue Get<TKey, TValue>(this Cache<TKey, TValue> cache, TKey key) {
         return cache[key];
      }

      public static TValue Put<TKey, TValue>(this Cache<TKey, TValue> cache, TKey key, TValue value) {
         return cache[key] = value;
      }
   }

   public interface Entry<TKey, TValue> {
      TKey Key { get; set; }
      TValue Value { get; set; }

      bool Exists { get; }
      bool IsDirty { get; set; }
   }

   public static class EntryExtensions {
      public static void FlagAsDirty<TKey, TValue>(Entry<TKey, TValue> entry) {
         entry.IsDirty = true;
      }
   }

   public interface EntryProcessor<TKey, TValue, TResult> {
      EntryOperationType Type { get; }
      TResult Process(Entry<TKey, TValue> entry);
   }
}
