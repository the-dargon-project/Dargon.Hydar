using Dargon.Hydar.Common;

namespace Dargon.Hydar.Cache.Data {
   public class EntryImpl<TKey, TValue> : Entry<TKey, TValue> {
      private readonly TKey key;
      private readonly bool exists;
      private TValue value;
      private bool isDirty;

      public EntryImpl(TKey key, TValue value, bool exists) {
         this.key = key;
         this.value = value;
         this.exists = exists;
      }

      public TKey Key => key;
      public bool Exists => exists;
      public TValue Value {  get { return this.value; } set { this.value = value; } }
      public bool IsDirty { get { return this.isDirty; } set { this.isDirty = true; } }
   }
}