using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grace.DependencyInjection;
using Grace.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Grace.AspNetCore.Hosting
{
    /// <summary>
    /// C# extensions for adding Grace as service container
    /// </summary>
    public static class GraceServiceProviderExtensions
    {
        /// <summary>
        /// This method is for ASP.Net Core 3.0
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IHostBuilder UseGrace(this IHostBuilder builder,
            IInjectionScopeConfiguration configuration = null)
        {
            return builder.UseServiceProviderFactory(new GraceServiceProviderFactory(configuration));
        }

        /// <summary>
        /// This should be used for ASP.Net 2.0 - 2.2
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IWebHostBuilder UseGrace(this IWebHostBuilder builder,
            IInjectionScopeConfiguration configuration = null)
        {
            return builder.ConfigureServices(c => c.AddSingleton<IServiceProviderFactory<IInjectionScope>>(new GraceServiceProviderFactory(configuration)));
        }

        private class GraceServiceProviderFactory : IServiceProviderFactory<IInjectionScope>
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
}
