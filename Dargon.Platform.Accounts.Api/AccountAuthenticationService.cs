﻿using System;

namespace Dargon.Platform.Accounts {
   public interface AccountAuthenticationService {
      bool TryAuthenticate(string username, string saltedPassword);
   }
}