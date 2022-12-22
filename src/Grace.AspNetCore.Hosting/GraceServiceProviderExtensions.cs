using Grace.DependencyInjection;
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
        public static IHostBuilder UseGrace(this IHostBuilder builder, IInjectionScopeConfiguration configuration = null)
        {
            return builder.UseServiceProviderFactory(new GraceServiceProviderFactory(configuration));
        }

        /// <summary>
        /// This should be used for ASP.Net 2.0 - 2.2
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IWebHostBuilder UseGrace(this IWebHostBuilder builder, IInjectionScopeConfiguration configuration = null)
        {
            return builder.ConfigureServices(c => c.AddSingleton<IServiceProviderFactory<IInjectionScope>>(new GraceServiceProviderFactory(configuration)));
        }
    }
}
