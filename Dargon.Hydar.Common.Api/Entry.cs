namespace Dargon.Hydar.Common {
   public interface Entry<TKey, TValue> {
      TKey Key { get; }
      TValue Value { get; set; }

      bool Exists { get; }
      bool IsDirty { get; set; }
      bool IsDeleted { get; set; }
   }

   public static class EntryExtensions {
      public static void FlagAsDirty<TKey, TValue>(this Entry<TKey, TValue> entry) {
         entry.IsDirty = true;
      }
      public static void Delete<TKey, TValue>(this Entry<TKey, TValue> entry) {
         entry.IsDirty = true;
         entry.IsDeleted = true;
      }
   }
}