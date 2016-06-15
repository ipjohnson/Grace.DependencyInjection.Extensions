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
        public static void Populate(
                IExportLocator exportLocator,
                IEnumerable<ServiceDescriptor> descriptors)
        {
            exportLocator.Configure(c =>
            {
                c.Export<GraceServiceProvider>().As<IServiceProvider>();
                c.Export<GraceServiceScopeFactory>().As<IServiceScopeFactory>();

                Register(c, descriptors);
            });
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
                    
                    c.AddExportStrategy(
                        new FuncInstanceExportStrategy(
                            descriptor.ServiceType,
                            lifeStyle,
                            descriptor.ImplementationFactory));
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

        private class GraceServiceScopeFactory : IServiceScopeFactory
        {
            private readonly IInjectionScope _injectionScope;

            public GraceServiceScopeFactory(IInjectionScope injectionScope)
            {
                _injectionScope = injectionScope;
            }

            public IServiceScope CreateScope()
            {
                return new GraceServiceScope(_injectionScope.CreateChildScope());
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
