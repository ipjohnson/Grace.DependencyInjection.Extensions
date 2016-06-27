using Grace.DependencyInjection;
using Grace.DependencyInjection.Impl;
using Grace.DependencyInjection.Impl.CompiledExport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Grace.AspNetCore.MVC.Inspectors
{
    public class ExportPropertyInspector : IExportStrategyInspector
    {
        public void Inspect(IExportStrategy exportStrategy)
        {
            ICompiledExportStrategy compiledExportStrategy = exportStrategy as ICompiledExportStrategy;

            // skip non compiled exports and controllers are they are bound by mvc
            if(compiledExportStrategy == null || 
               exportStrategy.ActivationType.GetTypeInfo().IsAssignableFrom(typeof(Controller)))
            {
                return;
            }

            foreach(var property in compiledExportStrategy.ActivationType.GetRuntimeProperties())
            {
                if(!property.CanWrite ||
                   !property.SetMethod.IsPublic)
                {
                    continue;
                }

                if(property.GetCustomAttributes().Any(a => a is IBindingSourceMetadata))
                {
                    compiledExportStrategy.ImportProperty(new ImportPropertyInfo { Property = property });
                }
            }
        }
    }
}
