using Grace.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Grace.AspNet.MVC.Inspectors
{
    public class FromAttributeValueProviderInspector : IInjectionValueProviderInspector
    {
        private IOptions<MvcOptions> _options;
        private IModelMetadataProvider _modelMetadataProvider;
        private IModelBinderFactory _modelBinderFactory;

        public IExportValueProvider GetValueProvider(IInjectionScope scope, IInjectionTargetInfo targetInfo, IExportValueProvider valueProvider, ExportStrategyFilter exportStrategyFilter, ILocateKeyValueProvider locateKey)
        {
            if(valueProvider != null ||
               targetInfo.InjectionDependencyType == ExportStrategyDependencyType.Property || 
              !targetInfo.InjectionTargetAttributes.Any(a => a is IBindingSourceMetadata))
            {
                return valueProvider;
            }

            ParameterInfo parameterInfo = targetInfo.InjectionTarget as ParameterInfo;
            
            if(_options == null)
            {
                scope.TryLocate(out _options);
            }

            if(_modelBinderFactory == null)
            {
                scope.TryLocate(out _modelBinderFactory);
            }

            if(_modelMetadataProvider == null)
            {
                scope.TryLocate(out _modelMetadataProvider);
            }

            if(_options != null && _modelBinderFactory != null && _modelMetadataProvider != null)
            {
                return new ParameterModelBinderValueProvider(_modelBinderFactory, _modelMetadataProvider, parameterInfo, targetInfo.InjectionTargetAttributes, _options);
            }

            return valueProvider;
        }
    }
}
