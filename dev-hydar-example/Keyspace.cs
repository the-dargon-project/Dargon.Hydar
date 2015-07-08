using ItzWarty;
using ItzWarty.Collections;
using System;
using System.Linq;
using SCG = System.Collections.Generic;

namespace Dargon.Hydar {
   public class Keyspace {
      private const long kTwoPow32 = 0x100000000L;
      private readonly int blockCount;
      private readonly int redundancy;

      public Keyspace(int blockCount, int redundancy) {
         this.blockCount = blockCount;
         this.redundancy = redundancy;
      }

      public int HashesPerBlock => (int)(kTwoPow32 / blockCount);

      public int HashToBlock(int hash) {
         return hash % blockCount;
      }

      public PartitionBlockInterval GetPartitionRange(int partitionId, int nodeCount) {
         int startBlockInclusive = (int)((((long)blockCount) * partitionId) / nodeCount);
         int endBlockExclusive = (int)((((long)blockCount) * (partitionId + 1)) / nodeCount);
         return new PartitionBlockInterval(startBlockInclusive, endBlockExclusive);
      }

      public PartitionBlockInterval[] GetNodePartitionRanges(int nodeRank, int nodeCount) {
         if (nodeCount < redundancy || nodeRank >= nodeCount || nodeRank < 0 || nodeCount < 0) {
            throw new ArgumentOutOfRangeException($"NodeRank {nodeRank} NodeCount {nodeCount} Redundancy {redundancy}.");
         }
         if (nodeCount == redundancy) {
            // Trivial Case (Optimize to prevent wraps)
            return new[] { new PartitionBlockInterval(0, blockCount) };
         } else if (nodeRank + redundancy <= nodeCount) {
            // Nonwrapping case, Not End
            var startBlockInclusive = GetPartitionRange(nodeRank, nodeCount).StartBlockInclusive;
            var endBlockExclusive = GetPartitionRange(nodeRank + redundancy - 1, nodeCount).EndBlockExclusive;
            return new[] { new PartitionBlockInterval(startBlockInclusive, endBlockExclusive) };
         } else {
            // Wrapping Case E.g. [0|1|2|3|4] for Node 4 w/ R=3, hPR=(4+3)%5=2, which starts at the end of 1
            var lowPartitionRange = GetPartitionRange(nodeRank, nodeCount);
            var highPartitionRange = GetPartitionRange((nodeRank + redundancy) % nodeCount, nodeCount);
            return new[] {
               new PartitionBlockInterval(lowPartitionRange.StartBlockInclusive, blockCount),
               new PartitionBlockInterval(0, highPartitionRange.StartBlockInclusive)
            };
         }
      }
   }
}
