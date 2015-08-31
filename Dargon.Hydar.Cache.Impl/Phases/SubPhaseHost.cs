using System.Diagnostics;
using Dargon.Hydar.Cache.PortableObjects;

namespace Dargon.Hydar.Cache.Phases {
   public class SubPhaseHost<TKey, TValue> : PhaseManagerImpl<TKey, TValue> {
      public override string Name => "leader_subphase";
      public CohortPhaseBase<TKey, TValue> Phase => (CohortPhaseBase<TKey, TValue>)currentPhase;

      public override void Transition(PhaseBase<TKey, TValue> phase) {
         Trace.Assert(phase is CohortPhaseBase<TKey, TValue>, "phase was not ICohortPhase");
         base.Transition(phase);
      }

      public bool IsInitialized => Phase != null;
      public CohortPartitioningState PartitioningState => Phase.PartitioningState;
   }
}
