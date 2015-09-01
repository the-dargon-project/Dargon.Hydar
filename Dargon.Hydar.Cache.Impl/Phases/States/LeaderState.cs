using System;
using ItzWarty.Collections;

namespace Dargon.Hydar.Cache.Phases.States {
   public class LeaderState<TKey, TValue> : EpochState<TKey, TValue> {
      public SubPhaseHost<TKey, TValue> SubPhaseHost { get; set; }
      public IReadOnlySet<Guid> PendingOutsiders { get; set; }
   }
}