using Dargon.Hydar.Common;

namespace Dargon.Hydar.Cache.Data.Entries {
   public class EntryImpl<TKey, TValue> : Entry<TKey, TValue> {
      private readonly TKey key;
      private TValue value;
      private bool exists;
      private bool isDirty;
      private bool isDeleted;

      public EntryImpl(TKey key, TValue value, bool exists) {
         this.key = key;
         this.value = value;
         this.exists = exists;
      }

      public TKey Key => key;
      public bool Exists { get { return exists; } set { exists = value; } }
      public TValue Value {  get { return this.value; } set { this.value = value; } }
      public bool IsDirty { get { return this.isDirty; } set { this.isDirty = value; } }
      public bool IsDeleted { get { return this.isDeleted; } set { this.isDeleted = value; } }
   }
}