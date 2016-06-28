using Grace.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Grace.AspNetCore.MVC.Inspectors
{
    public class FromAttributeValueProviderInspector : IInjectionValueProviderInspector
    {
        private IModelMetadataProvider _modelMetadataProvider;
        private IModelBinderFactory _modelBinderFactory;

        public IExportValueProvider GetValueProvider(IInjectionScope scope, IInjectionTargetInfo targetInfo, IExportValueProvider valueProvider, ExportStrategyFilter exportStrategyFilter, ILocateKeyValueProvider locateKey)
        {
            if (valueProvider != null ||
              !targetInfo.InjectionTargetAttributes.Any(a => a is IBindingSourceMetadata))
            {
                return valueProvider;
            }
            
            if (_modelBinderFactory == null)
            {
                scope.TryLocate(out _modelBinderFactory);
            }

            if (_modelMetadataProvider == null)
            {
                scope.TryLocate(out _modelMetadataProvider);
            }

            if (_modelBinderFactory != null && _modelMetadataProvider != null)
            {
                if (targetInfo.InjectionDependencyType == ExportStrategyDependencyType.Property)
                {
                    PropertyInfo propertyInfo = targetInfo.InjectionTarget as PropertyInfo;

                    return new ModelBinderValueProvider(propertyInfo, targetInfo.InjectionTargetAttributes, _modelBinderFactory, _modelMetadataProvider);
                }
                else
                {
                    ParameterInfo parameterInfo = targetInfo.InjectionTarget as ParameterInfo;

                    return new ModelBinderValueProvider(parameterInfo, targetInfo.InjectionTargetAttributes, _modelBinderFactory, _modelMetadataProvider);
                }
            }

            return valueProvider;
        }
    }
}
