using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Specification;
using System;

namespace Grace.DependencyInjection.Extensions.Tests
{
    public class GraceKeyedContainerTests : KeyedDependencyInjectionSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
        {
            return new DependencyInjectionContainer()
                .Populate(serviceCollection);
        }
    }
}