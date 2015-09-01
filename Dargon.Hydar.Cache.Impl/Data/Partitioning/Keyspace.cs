using System;
using System.Runtime.CompilerServices;
using Dargon.Hydar.Cache.PortableObjects;

namespace Dargon.Hydar.Cache.Data.Partitioning {
   public class Keyspace {
      [ThreadStatic] private static Random random;
      private const long kTwoPow32 = 0x100000000L;
      private readonly int blockCount;
      private readonly int redundancy;

      public Keyspace(int blockCount, int redundancy) {
         this.blockCount = blockCount;
         this.redundancy = redundancy;
      }

      public int BlockCount => blockCount;
      public int HashesPerBlock => (int)(kTwoPow32 / blockCount);
      private Random Random => random = random ?? new Random();

      public int HashToBlockId(int hash) {
         var mixedHash = Mix((uint)hash);
         return (int)((ulong)(mixedHash * blockCount) / kTwoPow32);
      }

      /// <summary>
      /// Robert Jenkins' 32 bit integer hash function.
      /// 
      /// See http://www.cris.com/~Ttwang/tech/inthash.htm 
      /// 
      /// Discovered via https://gist.github.com/badboy/6267743 
      ///            and http://burtleburtle.net/bob/hash/integer.html
      /// 
      /// Licensed under Public Domain.
      /// </summary>
      /// <param name="hash"></param>
      /// <returns></returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      private static uint Mix(uint hash) {
         hash = (hash + 0x7ed55d16) + (hash << 12);
         hash = (hash ^ 0xc761c23c) ^ (hash >> 19);
         hash = (hash + 0x165667b1) + (hash << 5);
         hash = (hash + 0xd3a2646c) ^ (hash << 9);
         hash = (hash + 0xfd7046c5) + (hash << 3);
         hash = (hash ^ 0xb55a4f09) ^ (hash >> 16);
         return hash;
      }

      public PartitionBlockInterval GetPartitionRange(int partitionId, int nodeCount) {
         uint startBlockInclusive = (uint)((((long)blockCount) * partitionId) / nodeCount);
         uint endBlockExclusive = (uint)((((long)blockCount) * (partitionId + 1)) / nodeCount);
         return new PartitionBlockInterval(startBlockInclusive, endBlockExclusive);
      }

      public PartitionBlockInterval[] GetNodePartitionRanges(int nodeRank, int nodeCount) {
         if (nodeCount < redundancy || nodeRank >= nodeCount || nodeRank < 0 || nodeCount < 0) {
            throw new ArgumentOutOfRangeException($"NodeRank {nodeRank} NodeCount {nodeCount} Redundancy {redundancy}.");
         }
         if (nodeCount == redundancy) {
            // Trivial Case (Optimize to prevent wraps)
            return new[] { new PartitionBlockInterval(0, (uint)blockCount) };
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
               new PartitionBlockInterval(lowPartitionRange.StartBlockInclusive, (uint)blockCount),
               new PartitionBlockInterval(0, highPartitionRange.StartBlockInclusive)
            };
         }
      }

      public int GetPeerIndex(int blockId, int nodeCount, bool masterOnly) {
         var partitionCount = nodeCount;
         var partitionId = blockId * partitionCount / blockCount;
         if (!masterOnly) {
            partitionId = (partitionId + Random.Next(0, redundancy)) % partitionCount;
         }
         return partitionId;
      }
   }
}
