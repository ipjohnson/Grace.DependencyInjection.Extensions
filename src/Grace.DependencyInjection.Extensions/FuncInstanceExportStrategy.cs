using Grace.Data.Immutable;
using Grace.DependencyInjection.Impl;
using Grace.DependencyInjection.Lifestyle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grace.DependencyInjection.Extensions
{
    public class FuncInstanceExportStrategy : IExportStrategy
    {
        private readonly Type _serviceType;
        private readonly Func<IServiceProvider, object> _serviceFactory;

        public FuncInstanceExportStrategy(Type serviceType, ILifestyle lifeStyle, Func<IServiceProvider, object> serviceFactory)
        {
            _serviceType = serviceType;
            Lifestyle = lifeStyle;
            _serviceFactory = serviceFactory;
        }

        /// <summary>
        /// Activate the export
        /// </summary>
        /// <param name="exportInjectionScope"></param>
        /// <param name="context"></param>
        /// <param name="consider"></param>
        /// <param name="locateKey"></param>
        /// <returns></returns>
        public object Activate(IInjectionScope exportInjectionScope, IInjectionContext context, ExportStrategyFilter consider, object locateKey)
        {
            if (Lifestyle != null)
            {
                return Lifestyle.Locate(InternalActivate, exportInjectionScope, context, this);
            }

            return InternalActivate(exportInjectionScope, context);
        }

        private object InternalActivate(IInjectionScope injectionScope, IInjectionContext context)
        {
            IInjectionScope scope = context.RequestingScope;

            var serviceProvider = scope.Locate<IServiceProvider>();

            return _serviceFactory(serviceProvider);
        }


        /// <summary>
        /// Dispose the func export, nothing to do
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Initialize the strategy
        /// </summary>
        public void Initialize()
        {
        }

        /// <summary>
        /// This is type that will be activated, can be used for filtering
        /// </summary>
        public Type ActivationType
        {
            get { return _serviceType; }
        }

        /// <summary>
        /// Usually the type.FullName, used for blacklisting purposes
        /// </summary>
        public string ActivationName
        {
            get { return _serviceType.FullName; }
        }

        /// <summary>
        /// Allows filter of strategy
        /// </summary>
        public bool AllowingFiltering
        {
            get { return false; }
        }

        /// <summary>
        /// Attributes associated with the export strategy. 
        /// Note: do not return null. Return an empty enumerable if there are none
        /// </summary>
        public IEnumerable<Attribute> Attributes
        {
            get { return ImmutableArray<Attribute>.Empty; }
        }

        /// <summary>
        /// The scope that owns this export
        /// </summary>
        public IInjectionScope OwningScope { get; set; }

        /// <summary>
        /// Export Key
        /// </summary>
        public object Key
        {
            get { return null; }
        }

        /// <summary>
        /// Names this strategy should be known as.
        /// </summary>
        public IEnumerable<string> ExportNames
        {
            get { return ImmutableArray<string>.Empty; }
        }

        /// <summary>
        /// Types this strategy should be known as
        /// </summary>
        public IEnumerable<Type> ExportTypes
        {
            get { yield return _serviceType; }
        }

        /// <summary>
        /// List of keyed interface to export under
        /// </summary>
        public IEnumerable<Tuple<Type, object>> KeyedExportTypes
        {
            get { return ImmutableArray<Tuple<Type, object>>.Empty; }
        }

        /// <summary>
        /// What export priority is this being exported as
        /// </summary>
        public int Priority
        {
            get { return -1; }
        }

        /// <summary>
        /// ILifestyle associated with export
        /// </summary>
        public ILifestyle Lifestyle { get; }

        /// <summary>
        /// Does this export have any conditions, this is important when setting up the strategy
        /// </summary>
        public bool HasConditions
        {
            get { return false; }
        }

        /// <summary>
        /// Are the object produced by this export externally owned
        /// </summary>
        public bool ExternallyOwned
        {
            get { return false; }
        }

        /// <summary>
        /// Does this export meet the conditions to be used
        /// </summary>
        /// <param name="injectionContext"></param>
        /// <returns></returns>
        public bool MeetsCondition(IInjectionContext injectionContext)
        {
            return true;
        }

        /// <summary>
        /// An export can specify it's own strategy
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IExportStrategy> SecondaryStrategies()
        {
            return ImmutableArray<IExportStrategy>.Empty;
        }

        /// <summary>
        /// Adds an enrich with delegate to the pipeline
        /// </summary>
        /// <param name="enrichWithDelegate"></param>
        public void EnrichWithDelegate(EnrichWithDelegate enrichWithDelegate)
        {

        }

        /// <summary>
        /// Doesn't depend on anything to construct
        /// </summary>
        public IEnumerable<ExportStrategyDependency> DependsOn
        {
            get { yield break; }
        }

        /// <summary>
        /// Metadata associated with this strategy
        /// </summary>
        public IExportMetadata Metadata
        {
            get { return new ExportMetadata(null); }
        }
    }
}
