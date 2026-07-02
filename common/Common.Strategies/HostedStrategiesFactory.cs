using Common.Utils.Reflection;
using Microsoft.Extensions.Logging;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Common.Strategies;

public class HostedStrategiesFactory : IHostedStrategiesFactory
{
    readonly ITypeResolver _resolver;
    readonly ILoggerFactory _loggerFactory;
    readonly List<StrategyTypeDescription> _strategyClasses = new();        

	public HostedStrategiesFactory(ITypeResolver typeResolver, ILoggerFactory loggerFactory)
	{
        _resolver = typeResolver;
        _loggerFactory = loggerFactory;

        _strategyClasses = typeResolver
            .LoadedStrategyAssemblies
            .SelectMany(a => a
                .GetTypes()
                .Where(t =>
                    t.IsClass
                    && !t.IsAbstract
                    && t.IsSubclassOf(typeof(AbstractHostedStrategy)))
                .Select(t =>
                {
                    var paramsType = t
                        .GetProperty("Params")!
                        .PropertyType;

                    var o = Activator.CreateInstance(paramsType);

                    return new StrategyTypeDescription
                    {
                        Name = t.Name,
                        FullName = t.FullName,
                        Params = paramsType
                            .GetProperties()
                            .ToDictionary(
                                p => p.Name,
                                p =>
                                {
                                    var v = p.GetValue(o); 
                                    return v == null ? "null" : v.ToString();
                                }
                            )                                
                    };
                })
            )
            .ToList();            
	}

    public AbstractHostedStrategy CreateHostedStrategy(Strategy config)
    {
        var type = _resolver.ResolveType(config.ClassName);
        var ahs = (AbstractHostedStrategy)Activator.CreateInstance(
            type,
            config
        );
        
        return ahs;
    }

    public IEnumerable<StrategyTypeDescription> SupportedStrategyClasses => _strategyClasses;
}