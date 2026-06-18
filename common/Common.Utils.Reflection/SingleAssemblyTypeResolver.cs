using System;
using System.Collections.Generic;
using System.Reflection;

namespace Common.Utils.Reflection;

public class SingleAssemblyTypeResolver : ITypeResolver
{
    Assembly _assembly;
    Assembly[] _assemblies;

    public SingleAssemblyTypeResolver(Assembly assembly)
    {
        _assembly = assembly;
        _assemblies = new Assembly[] { _assembly };
    }

    public IEnumerable<Assembly> LoadedAssemblies => _assemblies;

    public Type? ResolveType(string name)
    {
        var type = _assembly.GetType(name);
        return type;
    }
}