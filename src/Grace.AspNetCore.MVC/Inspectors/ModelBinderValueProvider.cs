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
        private readonly IReadOnlyList<IValueProviderFactory> _valueProviderFactories;

        public ModelBinderValueProvider(ParameterInfo parameterInfo, IEnumerable<object> attributes, IModelBinderFactory modelBinderFactory, IModelMetadataProvider modelMetadataProvider, IOptions<MvcOptions> optionsAccessor)
        {
            _cacheToken = parameterInfo;
            _modelName = parameterInfo.Name;
            _modelBinderFactory = modelBinderFactory;
            _binding = BindingInfo.GetBindingInfo(attributes);
            _modelMetadataProvider = modelMetadataProvider;
            _metadata = _modelMetadataProvider.GetMetadataForType(parameterInfo.ParameterType);
            _valueProviderFactories = optionsAccessor.Value.ValueProviderFactories.ToArray();
        }


        public ModelBinderValueProvider(PropertyInfo propertyInfo, IEnumerable<object> attributes, IModelBinderFactory modelBinderFactory, IModelMetadataProvider modelMetadataProvider, IOptions<MvcOptions> optionsAccessor)
        {
            _cacheToken = propertyInfo;
            _modelName = propertyInfo.Name;
            _modelBinderFactory = modelBinderFactory;
            _binding = BindingInfo.GetBindingInfo(attributes);
            _modelMetadataProvider = modelMetadataProvider;
            _metadata = _modelMetadataProvider.GetMetadataForType(propertyInfo.PropertyType);
            _valueProviderFactories = optionsAccessor.Value.ValueProviderFactories.ToArray();
        }
        
        public object Activate(IInjectionScope exportInjectionScope, IInjectionContext context, ExportStrategyFilter consider, object locateKey)
        {
            var binder = _modelBinderFactory.CreateBinder(new ModelBinderFactoryContext()
            {
                BindingInfo = _binding,
                Metadata = _metadata,
                CacheToken = _cacheToken,
            });

            var accessor = exportInjectionScope.Locate<IActionContextAccessor>();
            var controllerContext = new ControllerContext(accessor.ActionContext) { ValidatorProviders = new List<IModelValidatorProvider>() };

            var valueProviders = new List<IValueProvider>();
            var factoryContext = new ValueProviderFactoryContext(accessor.ActionContext);

            for (var i = 0; i < _valueProviderFactories.Count; i++)
            {
                var factory = _valueProviderFactories[i];
                var resultTask = factory.CreateValueProviderAsync(factoryContext);

                resultTask.Wait();
            }

            controllerContext.ValueProviders = factoryContext.ValueProviders;

            var modelBindingContext = DefaultModelBindingContext.CreateBindingContext(
                GetOperationBindingContext(controllerContext),
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

            binder.BindModelAsync(modelBindingContext).Wait();

            if (modelBindingContext.Result.HasValue &&
               modelBindingContext.Result.Value.IsModelSet)
            {
                return modelBindingContext.Result?.Model;
            }

            // if we can't match return the default value
            return context.TargetInfo.DefaultValue;
        }

        private OperationBindingContext GetOperationBindingContext(ControllerContext context)
        {
            return new OperationBindingContext
            {
                ActionContext = context,
                InputFormatters = context.InputFormatters,
                ValidatorProvider = new CompositeModelValidatorProvider(context.ValidatorProviders),
                MetadataProvider = _modelMetadataProvider,
                ValueProvider = new CompositeValueProvider(context.ValueProviders),
            };
        }

    }
}
