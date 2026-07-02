using System;
using System.Collections.Generic;
using System.Reflection;

namespace Common.Utils.Reflection;

public class SingleAssemblyTypeResolver : ITypeResolver
{
    Assembly _assembly;
    Assembly[] _strategyAssemblies;

    public SingleAssemblyTypeResolver(Assembly assembly)
    {
        _assembly = assembly;
        _strategyAssemblies = new Assembly[] { _assembly };
    }

    public IEnumerable<Assembly> LoadedStrategyAssemblies => _strategyAssemblies;

    public Type? ResolveType(string name)
    {
        var type = _assembly.GetType(name);
        return type;
    }
}