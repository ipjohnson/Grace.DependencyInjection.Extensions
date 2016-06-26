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

namespace Grace.AspNet.MVC.Inspectors
{
    public class ParameterModelBinderValueProvider : IExportValueProvider
    {
        private readonly ParameterInfo _parameterInfo;
        private readonly ModelMetadata _metadata;
        private readonly BindingInfo _binding;
        private readonly IModelBinderFactory _modelBinderFactory;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly IReadOnlyList<IValueProviderFactory> _valueProviderFactories;

        public ParameterModelBinderValueProvider(IModelBinderFactory modelBinderFactory, IModelMetadataProvider modelMetadataProvider,  ParameterInfo parameterInfo, IEnumerable<object> attributes, IOptions<MvcOptions> optionsAccessor)
        {
            _parameterInfo = parameterInfo;
            _modelBinderFactory = modelBinderFactory;
            _binding = BindingInfo.GetBindingInfo(attributes);
            _modelMetadataProvider = modelMetadataProvider;
            _metadata = _modelMetadataProvider.GetMetadataForType(parameterInfo.ParameterType);
            _valueProviderFactories = optionsAccessor.Value.ValueProviderFactories.ToArray();
        }

        public object Activate(IInjectionScope exportInjectionScope, IInjectionContext context, ExportStrategyFilter consider, object locateKey)
        {
            var binder = _modelBinderFactory.CreateBinder(new ModelBinderFactoryContext()
            {
                BindingInfo = _binding,
                Metadata = _metadata,
                CacheToken = _parameterInfo,
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
                _parameterInfo.Name);

            var parameterModelName = _binding.BinderModelName ?? _metadata.BinderModelName;
            if (parameterModelName != null)
            {
                // The name was set explicitly, always use that as the prefix.
                modelBindingContext.ModelName = parameterModelName;
            }
            else if (modelBindingContext.ValueProvider.ContainsPrefix(_parameterInfo.Name))
            {
                // We have a match for the parameter name, use that as that prefix.
                modelBindingContext.ModelName = _parameterInfo.Name;
            }
            else
            {
                // No match, fallback to empty string as the prefix.
                modelBindingContext.ModelName = string.Empty;
            }

            binder.BindModelAsync(modelBindingContext).Wait();
            
            return modelBindingContext.Result?.Model;
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
