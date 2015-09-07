using System;
using System.Threading;
using MicroLite;

namespace Dargon.Hydar.Cache.Data.Storage {
   public class MicroLiteCacheStore<TKey, TValue> : CacheStore<TKey, TValue> 
      where TValue : class, new() {
      private readonly object synchronization = new object();
      private readonly ISessionFactory sessionFactory;
      private ThreadLocal<ISession> session;

      public MicroLiteCacheStore(ISessionFactory sessionFactory) {
         this.sessionFactory = sessionFactory;
      }

      public ISession Session => GetOrCreateSession();

      private ISession GetOrCreateSession() {
         if (session.IsValueCreated) {
            return session.Value;
         } else {
            lock (synchronization) {
               if (session.IsValueCreated) {
                  return session.Value;
               } else {
                  session.Value = sessionFactory.OpenSession();
                  return session.Value;
               }
            }
         }
      }

      public bool TryGet(TKey key, out TValue value) {
         value = Session.Single<TValue>(key);
         return value != null;
      }

      public void Delete(TKey key) {
         if (!Session.Advanced.Delete(typeof(TValue), key)) {
            throw new InvalidOperationException($"Delete failed for key {key}.");
         }
      }

      public void Update(TKey key, TValue value) {
         if (!Session.Update(value)) {
            throw new InvalidOperationException($"Update failed for key {key} value {value}.");
         }
      }
   }
}