using Grace.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Grace.Extensions.Hosting
{
    /// <summary>
    /// C# extensions for adding Grace as a service container.
    /// </summary>
    public static class GraceServiceProviderExtensions
    {
        /// <summary>
        /// This method is for ASP.NET Core.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static IHostBuilder UseGrace(this IHostBuilder builder, IInjectionScopeConfiguration configuration = null)
        {
            return builder.UseServiceProviderFactory(new GraceServiceProviderFactory(configuration));
        }
    }
}
