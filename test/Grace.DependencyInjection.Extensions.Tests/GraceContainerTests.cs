using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Specification;
using System;

namespace Grace.DependencyInjection.Extensions.Tests
{
    public class GraceContainerTests : DependencyInjectionSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
        {
            DependencyInjectionContainer container = new DependencyInjectionContainer();

            return container.Populate(serviceCollection);
        }
    }
}
