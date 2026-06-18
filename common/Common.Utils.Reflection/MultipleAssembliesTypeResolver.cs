using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Common.Utils.Reflection;

public class MultipleAssembliesTypeResolver : ITypeResolver
{
    private readonly IEnumerable<Assembly> _assemblies;
    private readonly Dictionary<string, Type?> _cachedTypes = new();

    public MultipleAssembliesTypeResolver(IEnumerable<string> assemblies)
    {
        _assemblies = assemblies
            .Select(Assembly.Load)
            .ToList();
    }

    public MultipleAssembliesTypeResolver(IReadOnlyCollection<Assembly> assemblies)
    {
        _assemblies = assemblies;
    }

    public IEnumerable<Assembly> LoadedAssemblies => _assemblies;

    public Type? ResolveType(string name)
    {
        if (!_cachedTypes.TryGetValue(name, out var type))
        {
            foreach (var a in _assemblies)
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