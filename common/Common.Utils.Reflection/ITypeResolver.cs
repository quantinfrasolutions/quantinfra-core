using System;
using System.Collections.Generic;
using System.Reflection;

namespace Common.Utils.Reflection;

public interface ITypeResolver
{
    Type? ResolveType(string name);
    IEnumerable<Assembly> LoadedStrategyAssemblies { get; }
}