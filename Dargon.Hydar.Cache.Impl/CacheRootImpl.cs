using System;
using Dargon.Courier.Messaging;
using Dargon.Hydar.Cache.Data.Operations;
using Dargon.Hydar.Cache.Phases;
using Dargon.Hydar.Cache.Services;
using Dargon.Hydar.Client;
using Dargon.Hydar.Common;

namespace Dargon.Hydar.Cache {
   public class CacheRootImpl<TKey, TValue> : CacheRoot<TKey, TValue> {
      private readonly string cacheName;
      private readonly Guid id;
      private readonly PhaseManager<TKey, TValue> phaseManager;
      private readonly CacheService<TKey, TValue> cacheService;

      public CacheRootImpl(string cacheName, Guid id, PhaseManager<TKey, TValue> phaseManager, CacheService<TKey, TValue> cacheService) {
         this.cacheName = cacheName;
         this.id = id;
         this.phaseManager = phaseManager;
         this.cacheService = cacheService;
      }

      public string Name => cacheName;
      public Guid Id => id;

      public void Dispatch<T>(IReceivedMessage<T> message) {
         phaseManager.Dispatch(message);
      }

      #region Cache<TKey, TValue>
      public TResult Process<TResult>(TKey key, EntryProcessor<TKey, TValue, TResult> entryProcessor) {
         return cacheService.Process(key, entryProcessor);
      }

      public TResult Process<TResult, TProcessor>(TKey key, params object[] args) where TProcessor : EntryProcessor<TKey, TValue, TResult> {
         var processor = (TProcessor)Activator.CreateInstance(typeof(TProcessor), args);
         return Process(key, processor);
      }

      public bool TryGetValue(TKey key, out TValue value) {
         var result = cacheService.TryGet(key);
         value = result.Value;
         return result.Success;
      }

      public bool ContainsKey(TKey key) {
         TValue throwaway;
         return TryGetValue(key, out throwaway);
      }

      public TValue this[TKey key] { get { return cacheService.Get(key); } set { cacheService.Put(key, value); } }
      #endregion
   }
}