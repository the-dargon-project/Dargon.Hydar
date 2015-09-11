using Dargon.Ryu;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Diagnostics;
using System;
using System.Collections.Generic;

namespace Dargon.Platform.FrontendApplicationBase {
   public class RyuNancyBootstrapper : NancyBootstrapperBase<RyuContainer> {
      private readonly RyuContainer ryu;
      private bool modulesRegistered;

      public RyuNancyBootstrapper(RyuContainer ryu) {
         this.ryu = ryu;
      }

      protected override byte[] FavIcon => null;

      protected override IDiagnostics GetDiagnostics() {
         return ryu.Get<IDiagnostics>();
      }

      protected override IEnumerable<IApplicationStartup> GetApplicationStartupTasks() {
         return ryu.Find<IApplicationStartup>();
      }

      protected override IEnumerable<IRequestStartup> RegisterAndGetRequestStartupTasks(RyuContainer container, Type[] requestStartupTypes) {
         return ryu.Find<IRequestStartup>();
      }

      protected override IEnumerable<IRegistrations> GetRegistrationTasks() {
         return ryu.Find<IRegistrations>();
      }

      public override IEnumerable<INancyModule> GetAllModules(NancyContext context) {
         return ryu.Find<INancyModule>();
      }

      public override INancyModule GetModule(Type moduleType, NancyContext context) {
         return (INancyModule)ryu.Get(moduleType);
      }

      protected override INancyEngine GetEngineInternal() {
         return ryu.Get<INancyEngine>();
      }

      protected override RyuContainer GetApplicationContainer() {
         return ryu;
      }

      protected override void RegisterBootstrapperTypes(RyuContainer applicationContainer) {
         ryu.Set<INancyModuleCatalog>(this);
      }

      protected override void RegisterTypes(RyuContainer container, IEnumerable<TypeRegistration> typeRegistrations) {
         foreach (var registration in typeRegistrations) {
            HandleRegistration(registration.RegistrationType, registration.ImplementationType, registration.Lifetime);
         }
      }

      protected override void RegisterCollectionTypes(RyuContainer container, IEnumerable<CollectionTypeRegistration> collectionTypeRegistrations) {
         foreach (var registration in collectionTypeRegistrations) {
            foreach (var implementationType in registration.ImplementationTypes) {
               HandleRegistration(registration.RegistrationType, implementationType, registration.Lifetime);
            }
         }
      }

      protected override void RegisterModules(RyuContainer container, IEnumerable<ModuleRegistration> moduleRegistrationTypes) {
         if (modulesRegistered) {
            return;
         }

         modulesRegistered = true;

         foreach (var registration in moduleRegistrationTypes) {
            ryu.Get(registration.ModuleType);
         }
      }

      protected override void RegisterInstances(RyuContainer container, IEnumerable<InstanceRegistration> instanceRegistrations) {
         foreach (var registration in instanceRegistrations) {
            if (registration.Lifetime == Lifetime.Singleton) {
               ryu.Set(registration.RegistrationType, registration.Implementation);
            } else {
               throw new InvalidOperationException("Unsupported lifetime: " + registration.Lifetime);
            }
         }
      }

      private void HandleRegistration(Type registrationType, Type implementationType, Lifetime lifetime) {
         if (lifetime == Lifetime.Singleton) {
            var instance = ryu.Get(implementationType);
            ryu.Set(registrationType, instance);
            ryu.Set(implementationType, instance);
         } else {
            throw new InvalidOperationException("Unsupported lifetime: " + lifetime);
         }
      }
   }
}
