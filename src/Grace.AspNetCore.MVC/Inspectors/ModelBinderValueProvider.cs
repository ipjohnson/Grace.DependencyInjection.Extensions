using Grace.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Grace.AspNetCore.MVC.Inspectors
{
    public class ModelBinderValueProvider : IExportValueProvider
    {
        private readonly object _cacheToken;
        private readonly string _modelName;
        private readonly ModelMetadata _metadata;
        private readonly BindingInfo _binding;
        private readonly IModelBinderFactory _modelBinderFactory;
        private readonly IModelMetadataProvider _modelMetadataProvider;

        public ModelBinderValueProvider(ParameterInfo parameterInfo, IEnumerable<object> attributes, IModelBinderFactory modelBinderFactory, IModelMetadataProvider modelMetadataProvider)
        {
            _cacheToken = parameterInfo;
            _modelName = parameterInfo.Name;
            _modelBinderFactory = modelBinderFactory;
            _binding = BindingInfo.GetBindingInfo(attributes);
            _modelMetadataProvider = modelMetadataProvider;
            _metadata = _modelMetadataProvider.GetMetadataForType(parameterInfo.ParameterType);
        }


        public ModelBinderValueProvider(PropertyInfo propertyInfo, IEnumerable<object> attributes, IModelBinderFactory modelBinderFactory, IModelMetadataProvider modelMetadataProvider)
        {
            _cacheToken = propertyInfo;
            _modelName = propertyInfo.Name;
            _modelBinderFactory = modelBinderFactory;
            _binding = BindingInfo.GetBindingInfo(attributes);
            _modelMetadataProvider = modelMetadataProvider;
            _metadata = _modelMetadataProvider.GetMetadataForType(propertyInfo.PropertyType);
        }

        public object Activate(IInjectionScope exportInjectionScope, IInjectionContext context, ExportStrategyFilter consider, object locateKey)
        {
            var activateTask = ActivateAsync(exportInjectionScope, context, consider, locateKey);

            activateTask.Wait();

            if (activateTask.Status == TaskStatus.RanToCompletion)
            {
                return activateTask.Result;
            }
            else if (activateTask.Exception != null)
            {
                throw new Exception("Exception thrown while trying to bind to " + _modelName, activateTask.Exception);
            }
            else
            {
                return null;
            }
        }

        private async Task<object> ActivateAsync(IInjectionScope exportInjectionScope, IInjectionContext context, ExportStrategyFilter consider, object locateKey)
        {
            var binder = _modelBinderFactory.CreateBinder(new ModelBinderFactoryContext()
            {
                BindingInfo = _binding,
                Metadata = _metadata,
                CacheToken = _cacheToken,
            });

            var accessor = exportInjectionScope.Locate<IActionContextAccessor>();
            var controllerContext = new ControllerContext(accessor.ActionContext);

            var valueProvider = await CompositeValueProvider.CreateAsync(controllerContext);

            var modelBindingContext = DefaultModelBindingContext.CreateBindingContext(
                accessor.ActionContext,
                valueProvider,
                _metadata,
                _binding,
                _modelName);

            var parameterModelName = _binding.BinderModelName ?? _metadata.BinderModelName;
            if (parameterModelName != null)
            {
                // The name was set explicitly, always use that as the prefix.
                modelBindingContext.ModelName = parameterModelName;
            }
            else if (modelBindingContext.ValueProvider.ContainsPrefix(_modelName))
            {
                // We have a match for the parameter name, use that as that prefix.
                modelBindingContext.ModelName = _modelName;
            }
            else
            {
                // No match, fallback to empty string as the prefix.
                modelBindingContext.ModelName = string.Empty;
            }

            await binder.BindModelAsync(modelBindingContext);

            if (modelBindingContext.Result.IsModelSet)
            {
                return modelBindingContext.Result.Model;
            }

            // if we can't match return the default value
            return context.TargetInfo.DefaultValue;
        }
    }
}
