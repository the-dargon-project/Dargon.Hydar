﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Hydar.Common;
using Dargon.Platform.Accounts.Domain;

namespace Dargon.Platform.Accounts.Hydar.Processors {
   public interface AccountProcessorFactory {
      EntryProcessor<Guid, Account, bool> CreateAccount(string username, string password);
   }

   public class AccountProcessorFactoryImpl : AccountProcessorFactory {
      public EntryProcessor<Guid, Account, bool> CreateAccount(string username, string password) {
         return new AccountCreationProcessor(username, password);
      }
   }
}
