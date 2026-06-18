using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Common.Utils.Reflection;

public static class Extensions
{
    public static void AssertCompatible(this Assembly pluginAssembly)
    {
        foreach (var r in pluginAssembly.GetReferencedAssemblies()
                     .Where(a => a.Name!.StartsWith("QuantInfra.", StringComparison.OrdinalIgnoreCase)))
        {
            var loaded = AssemblyLoadContext.Default.Assemblies
                .FirstOrDefault(a => string.Equals(a.GetName().Name, r.Name, StringComparison.OrdinalIgnoreCase));

            if (loaded == null) continue;

            var lv = loaded.GetName().Version;
            if (lv != null && r.Version != null && lv != r.Version)
            {
                throw new InvalidOperationException(
                    $"Plugin expects {r.Name} {r.Version}, but host loaded {r.Name} {lv} from {loaded.Location}. " +
                    "Rebuild plugin/host with aligned package versions.");
            }
        }
    }
}