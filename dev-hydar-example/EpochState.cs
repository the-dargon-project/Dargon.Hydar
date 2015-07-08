using ItzWarty.Collections;
using System;
using System.Collections.Generic;

namespace Dargon.Hydar {
   public class EpochState {
      public Guid Leader { get; set; }
      
      /// <summary>This array is ordered.</summary>
      public Guid[] Participants { get; set; }
      public Keyspace Keyspace { get; set; }
   }
}
