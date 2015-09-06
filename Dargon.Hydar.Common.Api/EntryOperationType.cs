namespace Dargon.Hydar.Common {
   public enum EntryOperationType {
      Undefined = 0,
      Read = 0x01,
      Put = 0x02,
      Update = 0x04,
      ConditionalUpdate = 0x08,
      Delete = 0x10
   }
}
