﻿using System;
using System.Collections.Generic;
#if NET6_0_OR_GREATER
using System.Threading.Tasks;
#endif
using Microsoft.Extensions.DependencyInjection;
using Grace.DependencyInjection.Attributes;
using Grace.DependencyInjection.Attributes.Interfaces;

namespace Grace.DependencyInjection.Extensions
{
    /// <summary>
    /// static class for MVC registration
    /// </summary>
    public static class GraceRegistration
    {
        static GraceRegistration()
        {
            ImportAttributeInfo.RegisterImportAttributeAdapter<FromKeyedServicesAttribute>((attr, type, memberName) 
                => new ImportAttributeInfo { ImportKey = ((FromKeyedServicesAttribute)attr).Key });

            ImportAttributeInfo.RegisterImportAttributeAdapter<ServiceKeyAttribute>((attr, type, memberName) 
                => new ImportAttributeInfo { ImportKey = ImportKey.Key });
        }

        /// <summary>
        /// Populate a container with service descriptors
        /// </summary>
        /// <param name="exportLocator">export locator</param>
        /// <param name="descriptors">descriptors</param>
        public static IServiceProvider Populate(this IInjectionScope exportLocator,
            IEnumerable<ServiceDescriptor> descriptors)
        {
            exportLocator.Configure(c =>
            {
                c.Export<ServiceProviderIsServiceImpl>()
                    .As<IServiceProviderIsService>()
                    .As<IServiceProviderIsKeyedService>();

                c.ExcludeTypeFromAutoRegistration(nameof(Microsoft) + ".*");
                c.Export<GraceServiceProvider>().As<IServiceProvider>().ExternallyOwned();
                c.Export<GraceLifetimeScopeServiceScopeFactory>().As<IServiceScopeFactory>().Lifestyle.Singleton();
                Register(c, descriptors);
            });

            return exportLocator.Locate<IServiceProvider>();
        }

        private static void Register(IExportRegistrationBlock c, IEnumerable<ServiceDescriptor> descriptors)
        {
            foreach (var descriptor in descriptors)
            {
                if (descriptor.IsKeyedService)
                {
                    var key = descriptor.ServiceKey == KeyedService.AnyKey
                        ? ImportKey.Any
                        : descriptor.ServiceKey;

                    if (descriptor.KeyedImplementationType != null)
                    {
                        c.Export(descriptor.KeyedImplementationType)
                            .AsKeyed(descriptor.ServiceType, key)
                            .ConfigureLifetime(descriptor.Lifetime);
                    }
                    else if (descriptor.KeyedImplementationFactory != null)
                    {
                        // Adds [ImportKey] on second parameter so that Grace DelegateWrapperStrategy will pass the imported key
                        var factory = (IServiceProvider services, [ImportKey] object key) => 
                            descriptor.KeyedImplementationFactory(services, key);

                        c.ExportFactory(factory)
                            .AsKeyed(descriptor.ServiceType, key)
                            .ConfigureLifetime(descriptor.Lifetime);
                    }
                    else
                    {
                        c.ExportInstance(descriptor.KeyedImplementationInstance)
                            .AsKeyed(descriptor.ServiceType, key)
                            .ConfigureLifetime(descriptor.Lifetime);
                    }
                }
                else
                {
                    if (descriptor.ImplementationType != null)
                    {
                        c.Export(descriptor.ImplementationType)
                            .As(descriptor.ServiceType)
                            .ConfigureLifetime(descriptor.Lifetime);
                    }
                    else if (descriptor.ImplementationFactory != null)
                    {
                        c.ExportFactory(descriptor.ImplementationFactory)
                            .As(descriptor.ServiceType)
                            .ConfigureLifetime(descriptor.Lifetime);
                    }
                    else
                    {
                        c.ExportInstance(descriptor.ImplementationInstance)
                            .As(descriptor.ServiceType)
                            .ConfigureLifetime(descriptor.Lifetime);
                    }
                }
            }
        }

        private static IFluentExportStrategyConfiguration ConfigureLifetime(
            this IFluentExportStrategyConfiguration configuration, ServiceLifetime lifetime)
        {
            return lifetime switch
            {
                ServiceLifetime.Scoped => configuration.Lifestyle.SingletonPerScope(),
                ServiceLifetime.Singleton => configuration.Lifestyle.Singleton(),
                _ => configuration,
            };
        }

        private static IFluentExportInstanceConfiguration<T> ConfigureLifetime<T>(
            this IFluentExportInstanceConfiguration<T> configuration, ServiceLifetime lifecycleKind)
        {
            return lifecycleKind switch
            {
                ServiceLifetime.Scoped => configuration.Lifestyle.SingletonPerScope(),
                ServiceLifetime.Singleton => configuration.Lifestyle.Singleton(),
                _ => configuration,
            };
        }

        /// <summary>
        /// Service provider for Grace
        /// </summary>
        private class GraceServiceProvider
            : IKeyedServiceProvider
            , IDisposable
#if NET6_0_OR_GREATER
            , IAsyncDisposable
#endif
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

            public object GetKeyedService(Type serviceType, object serviceKey)
            {
                return _injectionScope.TryLocate(serviceType, out var service, withKey: serviceKey)
                    ? service
                    : null;
            }

            public object GetRequiredKeyedService(Type serviceType, object serviceKey)
            {
                return _injectionScope.Locate(serviceType, withKey: serviceKey);
            }

            /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
            public void Dispose()
            {
                _injectionScope.Dispose();
            }

#if NET6_0_OR_GREATER
            /// <summary>Asynchonously performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
            public ValueTask DisposeAsync()
            {
                return _injectionScope.DisposeAsync();
            }
#endif
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
#if NET6_0_OR_GREATER
            , IAsyncDisposable
#endif
        {
            private readonly GraceServiceProvider _serviceProvider;

            /// <summary>
            /// Service provider
            /// </summary>
            public IServiceProvider ServiceProvider => _serviceProvider;            

            /// <summary>
            /// Default constructor
            /// </summary>
            /// <param name="injectionScope"></param>
            public GraceServiceScope(IExportLocatorScope injectionScope)
            {
                // Need to wrap IServiceProvider to implement 
                // MS extensions interface IKeyedServiceProvider 
                _serviceProvider = new GraceServiceProvider(injectionScope);
            }

            // This code added to correctly implement the disposable pattern.
            public void Dispose() => _serviceProvider.Dispose();

#if NET6_0_OR_GREATER
            // This code added to correctly and asynchronously implement the disposable pattern.
            public ValueTask DisposeAsync() => _serviceProvider.DisposeAsync();
#endif
        }

        private class ServiceProviderIsServiceImpl : IServiceProviderIsKeyedService
        {
            private readonly IExportLocatorScope _exportLocatorScope;

            public ServiceProviderIsServiceImpl(IExportLocatorScope exportLocatorScope)
            {
                _exportLocatorScope = exportLocatorScope;
            }

            public bool IsService(Type serviceType)
            {
                return serviceType.IsGenericTypeDefinition
                    ? false
                    : _exportLocatorScope.CanLocate(serviceType);
            }

            public bool IsKeyedService(Type serviceType, object serviceKey)
            {
                return serviceType.IsGenericTypeDefinition 
                    ? false 
                    : _exportLocatorScope.CanLocate(serviceType, key: serviceKey);
            }
        }
    }
}