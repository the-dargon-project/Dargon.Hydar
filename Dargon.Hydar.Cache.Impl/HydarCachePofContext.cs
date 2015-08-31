using Dargon.Hydar.Cache.Data;
using Dargon.Hydar.Cache.PortableObjects;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Cache {
   public class HydarCachePofContext : PofContext {
      private const int kBasePofId = 2000;
      public HydarCachePofContext() {
         RegisterPortableObjectType(kBasePofId + 0, typeof(ElectionVoteDto));
         RegisterPortableObjectType(kBasePofId + 1, typeof(LeaderHeartbeatDto));
         RegisterPortableObjectType(kBasePofId + 2, typeof(CacheNeedDto));
         RegisterPortableObjectType(kBasePofId + 3, typeof(PartitionBlockInterval));
         RegisterPortableObjectType(kBasePofId + 4, typeof(OutsiderAnnounceDto));
         RegisterPortableObjectType(kBasePofId + 5, typeof(LeaderRepartitionSignalDto));
         RegisterPortableObjectType(kBasePofId + 6, typeof(CohortRepartitionCompletionDto));
         RegisterPortableObjectType(kBasePofId + 7, typeof(CohortHeartbeatDto));
         RegisterPortableObjectType(kBasePofId + 8, typeof(LeaderRepartitionCompletingDto));
         RegisterPortableObjectType(kBasePofId + 9, typeof(CacheHaveDto));
         RegisterPortableObjectType(kBasePofId + 10, typeof(BlockTransferResult));
         RegisterPortableObjectType(kBasePofId + 11, typeof(HydarServiceDescriptor));
         RegisterPortableObjectType(kBasePofId + 12, typeof(EntryOperationGet<,>));
         RegisterPortableObjectType(kBasePofId + 13, typeof(EntryOperationPut<,>));
         RegisterPortableObjectType(kBasePofId + 14, typeof(EntryOperationProcess<,,>));
      }
   }
}
