using System;
using System.Threading;
using Dargon.Hydar.Cache;
using Dargon.Platform.Accounts.Domain;
using MicroLite;
using MicroLite.Builder;

namespace Dargon.Platform.Accounts {
   public class AccountLookupServiceImpl : AccountLookupService {
      private readonly object synchronization = new object();
      private readonly ThreadLocal<ISession> session = new ThreadLocal<ISession>();
      private readonly ISessionFactory sessionFactory;
      private readonly Cache<string, Guid> accountIdByUsernameCache;

      public AccountLookupServiceImpl(Cache<string, Guid> accountIdByUsernameCache, ISessionFactory sessionFactory) {
         this.accountIdByUsernameCache = accountIdByUsernameCache;
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

      public bool TryGetAccountIdByUsername(string name, out Guid accountId) {
         if (accountIdByUsernameCache.TryGetValue(name, out accountId)) {
            return true;
         } else {
            var query = SqlBuilder.Select("*").From(typeof(Account)).Where("lower(username) = lower(@p0)", name).ToSqlQuery();
            var account = Session.Single<Account>(query);
            if (account == null) {
               accountId = Guid.Empty;
               return false;
            } else {
               accountId = account.Id;
               accountIdByUsernameCache.Put(name, accountId);
               return true;
            }
         }
      }
   }
}