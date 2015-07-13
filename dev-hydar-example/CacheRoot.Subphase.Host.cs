using System.Diagnostics;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
      public class SubPhaseHost : PhaseManagerImpl {
         public override string Name => "leader_subphase";
         public CohortPhaseBase Phase => (CohortPhaseBase)currentPhase;

         public override void Transition(PhaseBase phase) {
            Trace.Assert(phase is CohortPhaseBase, "phase was not ICohortPhase");
            base.Transition(phase);
         }

         public bool IsInitialized => Phase != null;
         public CohortPartitioningState PartitioningState => Phase.PartitioningState;
      }

   }
}
