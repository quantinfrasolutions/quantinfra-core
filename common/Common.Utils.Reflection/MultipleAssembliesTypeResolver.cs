using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Common.Utils.Reflection;

public class MultipleAssembliesTypeResolver : ITypeResolver
{
    private readonly IEnumerable<Assembly> _strategyAssemblies;
    private readonly Dictionary<string, Type?> _cachedTypes = new();

    public MultipleAssembliesTypeResolver(IEnumerable<string> assemblies)
    {
        _strategyAssemblies = assemblies
            .Select(Assembly.Load)
            .ToList();
    }

    public MultipleAssembliesTypeResolver(IReadOnlyCollection<Assembly> strategyAssemblies)
    {
        _strategyAssemblies = strategyAssemblies;
    }

    public IEnumerable<Assembly> LoadedStrategyAssemblies => _strategyAssemblies;

    public Type? ResolveType(string name)
    {
        if (!_cachedTypes.TryGetValue(name, out var type))
        {
            foreach (var a in _strategyAssemblies)
            {
                var t = a.GetType(name);
                if (t != null)
                {
                    type = t;
                    _cachedTypes[name] = type;
                    break;
                }
            }
        }
			
        return type;
    }
}