using System;
using Dargon.PortableObjects;

namespace Dargon.Platform.Accounts {
   public class UsernameTakenException : Exception, IPortableObject {
      public UsernameTakenException() : base("An account with the given username already exists.") {

      }

      public void Serialize(IPofWriter writer) { }
      public void Deserialize(IPofReader reader) { }
   }
}