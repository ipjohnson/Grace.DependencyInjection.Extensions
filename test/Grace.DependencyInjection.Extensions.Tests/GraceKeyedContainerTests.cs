using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Specification;
using System;

namespace Grace.DependencyInjection.Extensions.Tests
{
#if NET6_0_OR_GREATER
    public class GraceKeyedContainerTests : KeyedDependencyInjectionSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
        {
            DependencyInjectionContainer container = new DependencyInjectionContainer();

            return container.Populate(serviceCollection);
        }
    }
#endif
}