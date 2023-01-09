using Grace.DependencyInjection;
using Grace.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Grace.Extensions.Hosting
{
    public class GraceServiceProviderFactory : IServiceProviderFactory<IInjectionScope>
    {
        private readonly IInjectionScopeConfiguration _configuration;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="configuration"></param>
        public GraceServiceProviderFactory(IInjectionScopeConfiguration configuration)
        {
            _configuration = configuration ?? new InjectionScopeConfiguration();
        }

        /// <summary>
        /// Creates a container builder from an <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">The collection of services</param>
        /// <returns>A container builder that can be used to create an <see cref="T:System.IServiceProvider" />.</returns>
        public IInjectionScope CreateBuilder(IServiceCollection services)
        {
            var container = new DependencyInjectionContainer(_configuration);

            container.Populate(services);

            return container;
        }

        /// <summary>
        /// Creates an <see cref="T:System.IServiceProvider" /> from the container builder.
        /// </summary>
        /// <param name="containerBuilder">The container builder</param>
        /// <returns>An <see cref="T:System.IServiceProvider" /></returns>
        public IServiceProvider CreateServiceProvider(IInjectionScope containerBuilder)
        {
            return containerBuilder.Locate<IServiceProvider>();
        }
    }
}
