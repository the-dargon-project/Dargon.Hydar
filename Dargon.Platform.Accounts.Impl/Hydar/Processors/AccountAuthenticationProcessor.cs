using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Hydar.Common;
using Dargon.Platform.Accounts.Domain;
using Dargon.PortableObjects;

namespace Dargon.Platform.Accounts.Hydar.Processors {
   public class AccountAuthenticationProcessor : EntryProcessor<Guid, Account, bool> {
      private string saltedPassword;

      public AccountAuthenticationProcessor() { }

      public AccountAuthenticationProcessor(string saltedPassword) {
         this.saltedPassword = saltedPassword;
      }

      public EntryOperationType Type => EntryOperationType.ConditionalUpdate;

      public bool Process(Entry<Guid, Account> entry) {
         var account = entry.Value;
         if (account.Password.Equals(saltedPassword)) {
            account.LastLogin = DateTime.UtcNow;
            entry.FlagAsDirty();
            return true;
         } else {
            return false;
         }
      }

      public void Serialize(IPofWriter writer) {
         writer.WriteString(0, saltedPassword);
      }

      public void Deserialize(IPofReader reader) {
         saltedPassword = reader.ReadString(0);
      }
   }
}
