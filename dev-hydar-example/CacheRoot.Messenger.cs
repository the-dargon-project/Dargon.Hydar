using Dargon.Courier.Messaging;
using ItzWarty.Collections;
using System;
using System.Collections.Generic;

namespace Dargon.Hydar {
   public partial class CacheRoot<TKey, TValue> {
      public class Messenger {
         private readonly MessageSender messageSender;
         private readonly CacheConfiguration cacheConfiguration;

         public Messenger(MessageSender messageSender, CacheConfiguration cacheConfiguration) {
            this.messageSender = messageSender;
            this.cacheConfiguration = cacheConfiguration;
         }

         public MessageSender __MessageSender => messageSender;

         public void Vote(Guid nominee) {
            messageSender.SendBroadcast(new ElectionVoteDto(nominee, null));
         }

         public void Vote(Guid nominee, IReadOnlySet<Guid> followers) {
            messageSender.SendBroadcast(new ElectionVoteDto(nominee, followers));
         }

         public void LeaderHeartBeat(Guid id, Guid[] participants) {
            messageSender.SendBroadcast(new LeaderHeartbeatDto(id, participants));
         }

         public void CacheNeed(PartitionBlockInterval[] neededBlockIntervals) {
            messageSender.SendBroadcast(new CacheNeedDto(neededBlockIntervals));
         }

         public void CacheHave(PartitionBlockInterval[] haveBlockIntervals) {
            messageSender.SendBroadcast(new CacheHaveDto(haveBlockIntervals, cacheConfiguration.ServicePort));
         }

         public void OutsiderAnnounce() {
            messageSender.SendBroadcast(new OutsiderAnnounceDto());
         }

         public void LeaderRepartitionSignal(Guid epochId, Guid[] participantsOrdered) {
            messageSender.SendBroadcast(new LeaderRepartitionSignalDto(epochId, participantsOrdered));
         }

         public void CohortRepartitionCompletion() {
            messageSender.SendBroadcast(new CohortRepartitionCompletionDto());
         }

         public void LeaderRepartitionCompleting() {
            messageSender.SendBroadcast(new LeaderRepartitionCompletingDto());
         }

         public void CohortHeartBeat(Guid leaderId, Guid localIdentifier) {
            messageSender.SendUnreliableUnicast(leaderId, new CohortHeartbeatDto());
         }

         public Messenger WithMessageSender(MessageSender ms) => new Messenger(ms, cacheConfiguration);
      }

   }
}
