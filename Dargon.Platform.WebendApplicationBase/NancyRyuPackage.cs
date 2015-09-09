using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dargon.Ryu;
using ItzWarty;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.Cryptography;
using Nancy.Culture;
using Nancy.Diagnostics;
using Nancy.Diagnostics.Modules;
using Nancy.ErrorHandling;
using Nancy.Localization;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Responses.Negotiation;
using Nancy.Routing;
using Nancy.Routing.Constraints;
using Nancy.Routing.Trie;
using Nancy.Security;
using Nancy.Validation;
using Nancy.ViewEngines;
using Nancy.ViewEngines.SuperSimpleViewEngine;

namespace Dargon.Platform.FrontendApplicationBase {
   public class NancyRyuPackage : RyuPackageV1 {
      public NancyRyuPackage() {
         NancyConventions nancyConventions = new NancyConventions();
         new DefaultAcceptHeaderCoercionConventions().Initialise(nancyConventions);
         new DefaultCultureConventions().Initialise(nancyConventions);
         new DefaultStaticContentsConventions().Initialise(nancyConventions);
         new DefaultViewLocationConventions().Initialise(nancyConventions);

         Singleton<INancyModuleBuilder, DefaultNancyModuleBuilder>();
         Singleton<IViewFactory, DefaultViewFactory>();
         Singleton<IViewResolver, DefaultViewResolver>();
         Singleton<IViewLocator, DefaultViewLocator>();
         Singleton<ViewLocationConventions>(ryu => new ViewLocationConventions(nancyConventions.ViewLocationConventions));
         Singleton<IViewLocationProvider>(ryu => new ResourceViewLocationProvider());
         Singleton<IEnumerable<IViewEngine>>(ryu => ryu.Find<IViewEngine>());
         Singleton<IRenderContextFactory, DefaultRenderContextFactory>();
         Singleton<IViewCache, DefaultViewCache>();
         Singleton<ITextResource, ResourceBasedTextResource>();
         Singleton<IResourceAssemblyProvider, ResourceAssemblyProvider>();
         Singleton<IRootPathProvider, DefaultRootPathProvider>();
         Singleton<DefaultJsonSerializer>(RyuTypeFlags.Required);
         Singleton<DefaultXmlSerializer>(RyuTypeFlags.Required);
         Singleton<IEnumerable<ISerializer>>(ryu => ryu.Find<ISerializer>());
         Singleton<IResponseFormatterFactory, DefaultResponseFormatterFactory>();
         Singleton<IModelBinderLocator>(ryu => ryu.Get<DefaultModelBinderLocator>());
         Singleton<DefaultModelBinderLocator>(ryu => new DefaultModelBinderLocator(ryu.Find<IModelBinder>(), null));
         Singleton<IEnumerable<IModelValidatorFactory>>(ryu => ryu.Find<IModelValidatorFactory>());
         Singleton<IModelValidatorLocator, DefaultValidatorLocator>();
         Singleton<IRouteCache, RouteCache>();
         Singleton<INancyContextFactory, DefaultNancyContextFactory>();
         Singleton<CultureConventions>(ryu => new CultureConventions(nancyConventions.CultureConventions));
         Singleton<ICultureService, DefaultCultureService>();
         Singleton<IRequestTraceFactory, DefaultRequestTraceFactory>();
         Singleton<IRouteSegmentExtractor, DefaultRouteSegmentExtractor>();
         Singleton<IRouteDescriptionProvider, DefaultRouteDescriptionProvider>();
         Singleton<IEnumerable<IRouteMetadataProvider>>(ryu => ryu.Find<IRouteMetadataProvider>());
         Singleton<IRouteResolverTrie, RouteResolverTrie>();
         Singleton<ITrieNodeFactory, TrieNodeFactory>();
         IRouteSegmentConstraint[] constraints = new IRouteSegmentConstraint[] {
            new AlphaRouteSegmentConstraint(),
            new BoolRouteSegmentConstraint(),
            new CustomDateTimeRouteSegmentConstraint(),
            new DateTimeRouteSegmentConstraint(),
            new DecimalRouteSegmentConstraint(),
            new GuidRouteSegmentConstraint(),
            new IntRouteSegmentConstraint(),
            new LengthRouteSegmentConstraint(),
            new LongRouteSegmentConstraint(),
            new MaxLengthRouteSegmentConstraint(),
            new MaxRouteSegmentConstraint(),
            new MinLengthRouteSegmentConstraint(),
            new MinRouteSegmentConstraint(),
            new RangeRouteSegmentConstraint(),
            new VersionRouteSegmentConstraint()
         };
         constraints.ForEach(constraint => { Singleton(constraint.GetType(), ryu => constraint, RyuTypeFlags.Required); });
         Singleton<IEnumerable<IRouteSegmentConstraint>>(ryu => constraints);
         Singleton<JsonProcessor>();
         Singleton<ResponseProcessor>();
         Singleton<ViewProcessor>();
         Singleton<XmlProcessor>();
         Singleton<IEnumerable<IResponseProcessor>>(ryu => ryu.Find<IResponseProcessor>());
         Singleton<IRequestDispatcher, DefaultRequestDispatcher>();
         Singleton<IRouteInvoker, DefaultRouteInvoker>();
         Singleton<IResponseNegotiator, DefaultResponseNegotiator>();
         Singleton<AcceptHeaderCoercionConventions>(ryu => new AcceptHeaderCoercionConventions(nancyConventions.AcceptHeaderCoercionConventions));
         Singleton<DefaultStatusCodeHandler>();
         Singleton<IEnumerable<IStatusCodeHandler>>(ryu => ryu.Find<IStatusCodeHandler>());
         Singleton<IRequestTracing, DefaultRequestTracing>();
         Singleton<DiagnosticsConfiguration>(ryu => new DiagnosticsConfiguration());
         Singleton<IStaticContentProvider, DefaultStaticContentProvider>();
         Singleton<StaticContentsConventions>(ryu => new StaticContentsConventions(nancyConventions.StaticContentsConventions));
         Singleton<DefaultRouteCacheProvider>(ryu => new DefaultRouteCacheProvider(ryu.Get<IRouteCache>));
         Singleton<DefaultBinder>(ryu => new DefaultBinder(ryu.Find<ITypeConverter>(), ryu.Find<IBodyDeserializer>(), ryu.Get<IFieldNameConverter>(), ryu.Get<BindingDefaults>()));
         Singleton<IFieldNameConverter>(ryu => new DefaultFieldNameConverter());
         Singleton<FileSystemViewLocationProvider>(ryu => new FileSystemViewLocationProvider(ryu.Get<IRootPathProvider>()));
         Singleton<IEncryptionProvider, RijndaelEncryptionProvider>();
         Singleton<IKeyGenerator, RandomKeyGenerator>();
         Singleton<IHmacProvider, DefaultHmacProvider>();
         Singleton<DefaultDiagnostics>(ryu => new DefaultDiagnostics(
            ryu.Get<DiagnosticsConfiguration>(),
            ryu.Find<IDiagnosticsProvider>(),
            ryu.Get<IRootPathProvider>(),
            ryu.Get<IRequestTracing>(),
            ryu.Get<NancyInternalConfiguration>(),
            ryu.Get<IModelBinderLocator>(),
            ryu.Find<IResponseProcessor>(),
            ryu.Find<IRouteSegmentConstraint>(),
            ryu.Get<ICultureService>(),
            ryu.Get<IRequestTraceFactory>(),
            ryu.Find<IRouteMetadataProvider>(),
            ryu.Get<ITextResource>()
            ));
         Singleton<SuperSimpleViewEngineWrapper>(ryu => new SuperSimpleViewEngineWrapper(ryu.Find<ISuperSimpleViewEngineMatcher>()));
         Singleton<FavIconApplicationStartup>(ryu => new FavIconApplicationStartup(ryu.Get<IRootPathProvider>()));
         Singleton<JsonpApplicationStartup>(ryu => new JsonpApplicationStartup());
         Singleton<StaticContent>(ryu => new StaticContent(ryu.Get<IRootPathProvider>(), ryu.Get<StaticContentsConventions>()));
         Singleton<CsrfApplicationStartup>(ryu => new CsrfApplicationStartup(ryu.Get<CryptographyConfiguration>(), ryu.Get<IObjectSerializer>(), ryu.Get<ICsrfTokenValidator>()));
         Singleton<RootPathApplicationStartup>(ryu => new RootPathApplicationStartup(ryu.Get<IRootPathProvider>()));
         Singleton<ViewEngineApplicationStartup>(ryu => new ViewEngineApplicationStartup(ryu.Find<IViewEngine>(), ryu.Get<IViewCache>(), ryu.Get<IViewLocator>()));

         var moduleTypes = Assembly.GetExecutingAssembly().GetTypes().Where(FilterNancyModules);

         foreach (var moduleType in moduleTypes) {
            Singleton(moduleType, ryu => ryu.ForceConstruct(moduleType), RyuTypeFlags.Required);
         }
      }

      private static bool FilterNancyModules(Type type) {
         return !type.IsAbstract && typeof(NancyModule).IsAssignableFrom(type);
      }
   }
}