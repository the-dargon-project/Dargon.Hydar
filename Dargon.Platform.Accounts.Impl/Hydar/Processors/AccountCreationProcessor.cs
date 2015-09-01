using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Hydar.Common;
using Dargon.Platform.Accounts.Domain;
using Dargon.PortableObjects;

namespace Dargon.Platform.Accounts.Hydar.Processors {
   public class AccountCreationProcessor : EntryProcessor<Guid, Account, bool> {
      private string username;
      private string password;

      public AccountCreationProcessor() { }

      public AccountCreationProcessor(string username, string password) {
         this.username = username;
         this.password = password;
      }

      public EntryOperationType Type => EntryOperationType.ConditionalUpdate;

      public bool Process(Entry<Guid, Account> entry) {
         if (entry.Exists) {
            return false;
         } else {
            entry.Value = new Account {
               Id = entry.Key,
               Username = username,
               Password = password,
               Created = DateTime.UtcNow
            };
            entry.FlagAsDirty();
            return true;
         }
      }

      public void Serialize(IPofWriter writer) {
         writer.WriteString(0, username);
         writer.WriteString(1, password);
      }

      public void Deserialize(IPofReader reader) {
         username = reader.ReadString(0);
         password = reader.ReadString(1);
      }
   }
}
