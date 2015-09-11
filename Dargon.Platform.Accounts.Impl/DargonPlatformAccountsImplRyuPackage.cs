using Dargon.Platform.Accounts.Hydar;
using Dargon.Platform.Accounts.Hydar.Processors;
using Dargon.Platform.Accounts.Management;
using Dargon.Platform.Common.Cache;
using Dargon.Ryu;
using Dargon.Zilean;

namespace Dargon.Platform.Accounts {
   public class DargonPlatformAccountsImplRyuPackage : RyuPackageV1 {
      public DargonPlatformAccountsImplRyuPackage() {
         Singleton<Caches>();
         Singleton<AccountProcessorFactory, AccountProcessorFactoryImpl>();
         Singleton<AccountCreationService>((ryu) => {
            var chronokeeperService = ryu.Get<ChronokeeperService>();
            var caches = ryu.Get<Caches>();
            return new AccountCreationServiceImpl(chronokeeperService, caches.AccountCacheStore);
         });
         Singleton<AccountLookupService>((ryu) => {
            var platformCacheConfiguration = ryu.Get<PlatformCacheConfiguration>();
            var caches = ryu.Get<Caches>();
            return new AccountLookupServiceImpl(caches.AccountIdByUsernameCache, platformCacheConfiguration.DatabaseSessionFactory);
         });
         Singleton<AccountAuthenticationService>((ryu) => {
            var caches = ryu.Get<Caches>();
            var accountProcessorFactory = ryu.Get<AccountProcessorFactory>();
            var accountLookupService = ryu.Get<AccountLookupService>();
            return new AccountAuthenticationServiceImpl(caches.Account, caches.AccessTokenCache, accountProcessorFactory, accountLookupService);
         });
         LocalService<AccountService, AccountServiceProxyImpl>(RyuTypeFlags.None);
         PofContext<AccountsImplHydarPofContext>();
         Mob<AccountsMob>(RyuTypeFlags.None);
      }
   }
}
