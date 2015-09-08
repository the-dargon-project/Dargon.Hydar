using System.Runtime.InteropServices;

namespace Dargon.Platform.Accounts {
   [Guid("D448E090-F022-4942-8469-181AF655A6EF")]
   public interface AccountService : AccountCreationService, AccountAuthenticationService { }
}
