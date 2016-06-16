using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grace.DependencyInjection.Extensions.Tests
{
    public class GraceContainerTests : DependencyInjectionSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
        {
            DependencyInjectionContainer container = new DependencyInjectionContainer();

            return  container.CreateServiceProvider(serviceCollection);
        }
    }
}
