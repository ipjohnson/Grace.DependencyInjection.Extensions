﻿using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Grace.DependencyInjection.Extensions.Tests
{
    public class ServiceScopeFactoryTests
    {
        [Fact]
        public void ServiceScopeFactoryIsSingleton()
        {
            var serviceContainer = new DependencyInjectionContainer();

            var provider = serviceContainer.Populate(new ServiceCollection());

            var serviceScopeFactory1 = provider.GetRequiredService<IServiceScopeFactory>();
            var serviceScopeFactory2 = provider.GetRequiredService<IServiceScopeFactory>();

            Assert.Same(serviceScopeFactory1, serviceScopeFactory2);

            IServiceScopeFactory serviceScopeFactory3;
            using (var scope = provider.CreateScope())
            {
                serviceScopeFactory3 = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            }

            Assert.Same(serviceScopeFactory1, serviceScopeFactory3);
        }

        [Fact]
        public void ServiceScopesAreFlat()
        {
            var serviceContainer = new DependencyInjectionContainer();

            var provider = serviceContainer.Populate(new ServiceCollection());

            using (var scope = provider.CreateScope())
            {
                var exportLocator = scope.ServiceProvider.GetRequiredService<IExportLocatorScope>();
                Assert.Same(serviceContainer, exportLocator.Parent);

                using (var nestedScope = scope.ServiceProvider.CreateScope())
                {
                    var exportLocator2 = nestedScope.ServiceProvider.GetRequiredService<IExportLocatorScope>();
                    Assert.Same(serviceContainer, exportLocator2.Parent);
                }
            }
        }
    }
}
