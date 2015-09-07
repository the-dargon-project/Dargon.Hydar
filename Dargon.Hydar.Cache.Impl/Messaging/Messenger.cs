using System;
using Dargon.Courier.Messaging;
using Dargon.Hydar.Cache.PortableObjects;
using ItzWarty.Collections;

namespace Dargon.Hydar.Cache.Messaging {
   public class Messenger<TKey, TValue> {
      private readonly Guid cacheId;
      private readonly MessageSender messageSender;
      private readonly CacheConfiguration<TKey, TValue> cacheConfiguration;

      public Messenger(Guid cacheId, MessageSender messageSender, CacheConfiguration<TKey, TValue> cacheConfiguration) {
         this.cacheId = cacheId;
         this.messageSender = messageSender;
         this.cacheConfiguration = cacheConfiguration;
      }

      public MessageSender __MessageSender => messageSender;

      public void Vote(Guid nominee) {
         messageSender.SendBroadcast(new ElectionVoteDto(cacheId, nominee, null));
      }

      public void Vote(Guid nominee, IReadOnlySet<Guid> followers) {
         messageSender.SendBroadcast(new ElectionVoteDto(cacheId, nominee, followers));
      }

      public void LeaderHeartBeat(Guid epochId, Guid[] participants) {
         messageSender.SendBroadcast(new LeaderHeartbeatDto(cacheId, epochId, participants));
      }

      public void CacheNeed(PartitionBlockInterval[] neededBlockIntervals) {
         messageSender.SendBroadcast(new CacheNeedDto(cacheId, neededBlockIntervals));
      }

      public void CacheHave(PartitionBlockInterval[] haveBlockIntervals) {
         messageSender.SendBroadcast(new CacheHaveDto(cacheId, haveBlockIntervals, cacheConfiguration.ServicePort));
      }

      public void OutsiderAnnounce() {
         messageSender.SendBroadcast(new OutsiderAnnounceDto(cacheId));
      }

      public void LeaderRepartitionSignal(Guid epochId, Guid[] participantsOrdered) {
         messageSender.SendBroadcast(new LeaderRepartitionSignalDto(cacheId, epochId, participantsOrdered));
      }

      public void CohortRepartitionCompletion() {
         messageSender.SendBroadcast(new CohortRepartitionCompletionDto(cacheId));
      }

      public void LeaderRepartitionCompleting() {
         messageSender.SendBroadcast(new LeaderRepartitionCompletingDto(cacheId));
      }

      public void CohortHeartBeat(Guid leaderId, Guid localIdentifier) {
         messageSender.SendUnreliableUnicast(leaderId, new CohortHeartbeatDto(cacheId));
      }

      public Messenger<TKey, TValue> WithMessageSender(MessageSender ms) => new Messenger<TKey, TValue>(cacheId, ms, cacheConfiguration);
   }
}
