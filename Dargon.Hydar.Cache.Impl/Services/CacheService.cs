namespace Dargon.Hydar.Cache.Services {
   public interface CacheService<TKey, TValue> : ClientCacheService<TKey, TValue>, InterCacheService<TKey, TValue> { }
}