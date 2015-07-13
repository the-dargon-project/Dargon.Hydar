using Dargon.Courier.Identities;
using Dargon.Courier.Messaging;
using ItzWarty;
using ItzWarty.Collections;
using System;
using System.Diagnostics;
using System.Linq;
using SCG = System.Collections.Generic;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
      private readonly CourierEndpointImpl endpoint;
      private readonly MessageSenderImpl messageSender;
      private readonly CacheConfiguration cacheConfiguration;

      public CacheRoot(CourierEndpointImpl endpoint, MessageSenderImpl messageSender, CacheConfiguration cacheConfiguration) {
         this.endpoint = endpoint;
         this.messageSender = messageSender;
         this.cacheConfiguration = cacheConfiguration;
      }
   }
}