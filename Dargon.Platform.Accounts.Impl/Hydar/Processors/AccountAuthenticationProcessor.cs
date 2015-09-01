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
      private string password;

      public AccountAuthenticationProcessor() { }

      public AccountAuthenticationProcessor(string password) {
         this.password = password;
      }

      public EntryOperationType Type => EntryOperationType.ConditionalUpdate;

      public bool Process(Entry<Guid, Account> entry) {
         var account = entry.Value;
         if (!account.Password.Equals(password)) {
            account.LastLogin = DateTime.UtcNow;
            entry.FlagAsDirty();
            return true;
         } else {
            return false;
         }
      }

      public void Serialize(IPofWriter writer) {
         writer.WriteString(0, password);
      }

      public void Deserialize(IPofReader reader) {
         password = reader.ReadString(0);
      }
   }
}
