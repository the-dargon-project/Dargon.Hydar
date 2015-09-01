using Dargon.Platform.Accounts.Hydar;
using Dargon.Platform.Accounts.Hydar.Processors;
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
            var accountProcessorFactory = ryu.Get<AccountProcessorFactory>();
            return new AccountCreationServiceImpl(chronokeeperService, caches.Account, accountProcessorFactory);
         });
         LocalService<AccountService, AccountServiceProxyImpl>(RyuTypeFlags.None);
         PofContext<AccountsImplHydarPofContext>();
      }
   }
}
