using Dargon.Courier.Messaging;
using ItzWarty.Collections;
using System;
using System.Collections.Generic;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
      public class Messenger {
         private readonly Guid cacheId;
         private readonly MessageSender messageSender;
         private readonly CacheConfiguration cacheConfiguration;

         public Messenger(Guid cacheId, MessageSender messageSender, CacheConfiguration cacheConfiguration) {
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

         public Messenger WithMessageSender(MessageSender ms) => new Messenger(cacheId, ms, cacheConfiguration);
      }

   }
}
