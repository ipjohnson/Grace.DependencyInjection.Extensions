using Grace.DependencyInjection.Lifestyle;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grace.DependencyInjection.Extensions
{
    public static class GraceRegistration
    {
        /// <summary>
        /// Populate a container with service descriptors
        /// </summary>
        /// <param name="exportLocator">export locator</param>
        /// <param name="descriptors">descriptors</param>
        /// <param name="registrationDelegate">allows you to specify registration into child container (usually null)</param>
        public static IServiceProvider CreateServiceProvider(
                this IExportLocator exportLocator,
                IEnumerable<ServiceDescriptor> descriptors,
                ExportRegistrationDelegate registrationDelegate = null)
        {
            if (registrationDelegate == null)
            {
                exportLocator.Configure(c =>
                {
                    c.Export<GraceServiceProvider>().As<IServiceProvider>();
                    c.Export<GraceLifetimeScopeServiceScopeFactory>().As<IServiceScopeFactory>();
                    Register(c, descriptors);
                });
            }
            else
            {
                exportLocator.Configure(c =>
                {
                    c.Export<GraceServiceProvider>().As<IServiceProvider>();
                    c.ExportInstance((scope, context) => new GraceChildScopeServiceScopeFactory(scope, registrationDelegate)).As<IServiceScopeFactory>();
                    Register(c, descriptors);
                });
            }

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
                    ILifestyle lifeStyle = null;

                    switch (descriptor.Lifetime)
                    {
                        case ServiceLifetime.Singleton:
                            lifeStyle = new SingletonLifestyle();
                            break;
                        case ServiceLifetime.Scoped:
                            lifeStyle = new SingletonPerScopeLifestyle();
                            break;
                    }
                    
                    c.ExportInstance((scope,context) => descriptor.ImplementationFactory(new GraceServiceProvider(scope))).
                        As(descriptor.ServiceType).
                        ConfigureLifetime(descriptor.Lifetime);
                }
                else
                {
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

        private class GraceServiceProvider : IServiceProvider, ISupportRequiredService
        {
            private readonly IInjectionScope _injectionScope;

            public GraceServiceProvider(IInjectionScope injectionScope)
            {
                _injectionScope = injectionScope;
            }

            public object GetRequiredService(Type serviceType)
            {
                return _injectionScope.Locate(serviceType);
            }

            public object GetService(Type serviceType)
            {
                object returnValue;

                _injectionScope.TryLocate(serviceType, out returnValue);

                return returnValue;
            }
        }

        private class GraceLifetimeScopeServiceScopeFactory : IServiceScopeFactory
        {
            private readonly IInjectionScope _injectionScope;

            public GraceLifetimeScopeServiceScopeFactory(IInjectionScope injectionScope)
            {
                _injectionScope = injectionScope;
            }

            public IServiceScope CreateScope()
            {
                return new GraceServiceScope(_injectionScope.BeginLifetimeScope());
            }
        }

        public class GraceChildScopeServiceScopeFactory  : IServiceScopeFactory
        {
            private readonly IInjectionScope _injectionScope;
            private readonly ExportRegistrationDelegate _registrationDelegate;

            public GraceChildScopeServiceScopeFactory(IInjectionScope injectionScope, ExportRegistrationDelegate registrationDelegate)
            {
                _injectionScope = injectionScope;
                _registrationDelegate = registrationDelegate;
            }

            public IServiceScope CreateScope()
            {
                return new GraceServiceScope(_injectionScope.CreateChildScope(_registrationDelegate));
            }
        }

        private class GraceServiceScope : IServiceScope
        {
            private IInjectionScope _injectionScope;
            private readonly IServiceProvider _serviceProvider;
            private bool disposedValue = false; // To detect redundant calls

            public GraceServiceScope(IInjectionScope injectionScope)
            {
                _injectionScope = injectionScope;
                _serviceProvider = _injectionScope.Locate<IServiceProvider>();
            }

            public IServiceProvider ServiceProvider
            {
                get { return _serviceProvider; }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        _injectionScope.Dispose();
                    }

                    disposedValue = true;
                }
            }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
                // TODO: tell GC not to call its finalizer when the above finalizer is overridden.
                // GC.SuppressFinalize(this);
            }
        }
    }
}
