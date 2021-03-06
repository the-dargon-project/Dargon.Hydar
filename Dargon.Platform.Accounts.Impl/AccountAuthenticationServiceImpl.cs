﻿using System;
using Dargon.Hydar.Cache;
using Dargon.Platform.Accounts.Domain;
using Dargon.Platform.Accounts.Hydar.Processors;

namespace Dargon.Platform.Accounts {
   public class AccountAuthenticationServiceImpl : AccountAuthenticationService {
      private readonly Cache<Guid, Account> accountCache;
      private readonly Cache<string, Guid> accessTokenCache;
      private readonly AccountProcessorFactory accountProcessorFactory;
      private readonly AccountLookupService accountLookupService;

      public AccountAuthenticationServiceImpl(Cache<Guid, Account> accountCache, Cache<string, Guid> accessTokenCache, AccountProcessorFactory accountProcessorFactory, AccountLookupService accountLookupService) {
         this.accountCache = accountCache;
         this.accessTokenCache = accessTokenCache;
         this.accountProcessorFactory = accountProcessorFactory;
         this.accountLookupService = accountLookupService;
      }

      public bool TryAuthenticate(string username, string saltedPassword, out Guid accountId, out string accessToken) {
         if (!accountLookupService.TryGetAccountIdByUsername(username, out accountId)) {
            accessToken = null;
            return false;
         } else {
            Console.WriteLine("TGAIBU: " + accountId);
            var authenticationSuccessful = accountCache.Process(accountId, accountProcessorFactory.Authenticate(saltedPassword));
            if (!authenticationSuccessful) {
               accountId = Guid.Empty;
            }
            accessToken = Guid.NewGuid().ToString("n");
            accessTokenCache.Put(accessToken, accountId);
            return authenticationSuccessful;
         }
      }

      public bool TryValidateToken(string accessToken, out Guid accountId) {
         return accessTokenCache.TryGetValue(accessToken, out accountId);
      }
   }
}