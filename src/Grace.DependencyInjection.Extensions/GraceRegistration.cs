using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
#if NETSTANDARD2_0
using Microsoft.Extensions.Options;
#endif

namespace Grace.DependencyInjection.Extensions
{
    /// <summary>
    /// static class for MVC registration
    /// </summary>
    public static class GraceRegistration
    {
        /// <summary>
        /// Populate a container with service descriptors
        /// </summary>
        /// <param name="exportLocator">export locator</param>
        /// <param name="descriptors">descriptors</param>
        public static IServiceProvider Populate(this IInjectionScope exportLocator, IEnumerable<ServiceDescriptor> descriptors)
        {
            exportLocator.Configure(c =>
            {
                c.ExcludeTypeFromAutoRegistration(nameof(Microsoft) + ".*");
                c.Export<GraceServiceProvider>().As<IServiceProvider>().ExternallyOwned();
                c.Export<GraceLifetimeScopeServiceScopeFactory>().As<IServiceScopeFactory>();
                Register(c, descriptors);
            });

            return exportLocator.Locate<IServiceProvider>();
        }

        private static void Register(IExportRegistrationBlock c, IEnumerable<ServiceDescriptor> descriptors)
        {
            foreach (var descriptor in descriptors)
            {
                if (descriptor.ImplementationType != null)
                {
                    c.Export(descriptor.ImplementationType).
                      As(descriptor.ServiceType).
                      ConfigureLifetime(descriptor.Lifetime);
                }
                else if (descriptor.ImplementationFactory != null)
                {
                    c.ExportFactory(descriptor.ImplementationFactory).
                        As(descriptor.ServiceType).
                        ConfigureLifetime(descriptor.Lifetime);
                }
                else
                {
#if NETSTANDARD2_0
                    var instanceType = descriptor.ImplementationInstance.GetType();

                    if (instanceType.IsConstructedGenericType)
                    {
                        var type = instanceType;
                        while (type != null && type.IsConstructedGenericType)
                        {
                            if (type.GetGenericTypeDefinition() == typeof(ConfigureNamedOptions<>))
                            {
                                break;
                            }

                            type = type.BaseType;
                        }

                        if (type != null && type.IsConstructedGenericType)
                        {
                            var nameProperty = instanceType.GetProperty("Name");
                            var name = nameProperty?.GetValue(descriptor.ImplementationInstance)?.ToString();

                            // we only want to export as keyed if they are named
                            if (!string.IsNullOrEmpty(name))
                            {
                                c.ExportInstance(descriptor.ImplementationInstance)
                                    .AsKeyed(descriptor.ServiceType, name)
                                    .ConfigureLifetime(descriptor.Lifetime);
                                continue;
                            }
                        }
                    }
#endif
                    c.ExportInstance(descriptor.ImplementationInstance).
                      As(descriptor.ServiceType).
                      ConfigureLifetime(descriptor.Lifetime);
                }
            }
        }

        private static IFluentExportStrategyConfiguration ConfigureLifetime(this IFluentExportStrategyConfiguration configuration, ServiceLifetime lifetime)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Scoped:
                    return configuration.Lifestyle.SingletonPerScope();

                case ServiceLifetime.Singleton:
                    return configuration.Lifestyle.Singleton();
            }

            return configuration;
        }

        private static IFluentExportInstanceConfiguration<T> ConfigureLifetime<T>(this IFluentExportInstanceConfiguration<T> configuration, ServiceLifetime lifecycleKind)
        {
            switch (lifecycleKind)
            {
                case ServiceLifetime.Scoped:
                    return configuration.Lifestyle.SingletonPerScope();

                case ServiceLifetime.Singleton:
                    return configuration.Lifestyle.Singleton();
            }

            return configuration;
        }

        /// <summary>
        /// Service provider for Grace
        /// </summary>
        private class GraceServiceProvider : IServiceProvider, IDisposable
        {
            private readonly IExportLocatorScope _injectionScope;

            /// <summary>
            /// Default constructor
            /// </summary>
            /// <param name="injectionScope"></param>
            public GraceServiceProvider(IExportLocatorScope injectionScope)
            {
                _injectionScope = injectionScope;
            }

            /// <summary>Gets the service object of the specified type.</summary>
            /// <returns>A service object of type <paramref name="serviceType" />.-or- null if there is no service object of type <paramref name="serviceType" />.</returns>
            /// <param name="serviceType">An object that specifies the type of service object to get. </param>
            /// <filterpriority>2</filterpriority>
            public object GetService(Type serviceType)
            {
                return _injectionScope.LocateOrDefault(serviceType, null);
            }

            /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
            public void Dispose()
            {
                _injectionScope.Dispose();
            }
        }

        /// <summary>
        /// Service scope factory that uses grace
        /// </summary>
        private class GraceLifetimeScopeServiceScopeFactory : IServiceScopeFactory
        {
            private readonly IExportLocatorScope _injectionScope;

            /// <summary>
            /// Default constructor
            /// </summary>
            /// <param name="injectionScope"></param>
            public GraceLifetimeScopeServiceScopeFactory(IExportLocatorScope injectionScope)
            {
                _injectionScope = injectionScope;
            }

            /// <summary>
            /// Create an <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceScope" /> which
            /// contains an <see cref="T:System.IServiceProvider" /> used to resolve dependencies from a
            /// newly created scope.
            /// </summary>
            /// <returns>
            /// An <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceScope" /> controlling the
            /// lifetime of the scope. Once this is disposed, any scoped services that have been resolved
            /// from the <see cref="P:Microsoft.Extensions.DependencyInjection.IServiceScope.ServiceProvider" />
            /// will also be disposed.
            /// </returns>
            public IServiceScope CreateScope()
            {
                return new GraceServiceScope(_injectionScope.BeginLifetimeScope());
            }
        }

        /// <summary>
        /// Grace service scope
        /// </summary>
        private class GraceServiceScope : IServiceScope
        {
            private readonly IExportLocatorScope _injectionScope;

            /// <summary>
            /// Default constructor
            /// </summary>
            /// <param name="injectionScope"></param>
            public GraceServiceScope(IExportLocatorScope injectionScope)
            {
                _injectionScope = injectionScope;
#if NETSTANDARD1_0
                ServiceProvider = new GraceServiceProvider(injectionScope);
#else
                ServiceProvider = injectionScope;
#endif
            }

            /// <summary>
            /// Service provider
            /// </summary>
            public IServiceProvider ServiceProvider { get; }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                _injectionScope.Dispose();
            }
        }
    }
}
