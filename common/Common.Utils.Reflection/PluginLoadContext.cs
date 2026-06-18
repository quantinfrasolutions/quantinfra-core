using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Common.Utils.Reflection;

public class PluginLoadContext : AssemblyLoadContext
{
    private AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }
    
    public static void PreloadQuantInfraAssemblies()
    {
        var baseDir = AppContext.BaseDirectory;

        foreach (var path in Directory.EnumerateFiles(baseDir, "*.dll"))
        {
            try
            {
                // Avoid re-loading if already loaded
                var name = AssemblyName.GetAssemblyName(path);
                var already = AssemblyLoadContext.Default.Assemblies
                    .Any(a => string.Equals(a.GetName().Name, name.Name, StringComparison.OrdinalIgnoreCase));

                if (!already)
                    AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            }
            catch
            {
                // ignore non-.NET dlls or load failures if any
            }
        }
    }
    
    protected override Assembly Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name!;

        // 1) Always share your own assemblies from the host (Default ALC)
        //    so you never get two copies of QuantInfra.* across contexts.
        if (name.StartsWith("QuantInfra.", StringComparison.OrdinalIgnoreCase))
        {
            // If already loaded in Default, reuse it
            var already = AssemblyLoadContext.Default.Assemblies
                .FirstOrDefault(a => string.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase));
            if (already != null)
                return already;

            // Otherwise force-load the host copy into Default ALC from the host base dir
            var hostPath = Path.Combine(AppContext.BaseDirectory, name + ".dll");
            if (File.Exists(hostPath))
                return AssemblyLoadContext.Default.LoadFromAssemblyPath(hostPath);

            // If host doesn't have it, you probably packaged wrong (shared DLL ended up only in plugin)
            throw new FileNotFoundException(
                $"Shared assembly '{name}' must be provided by the host (in {AppContext.BaseDirectory}).");
        }

        // 2) If default already loaded something else (framework libs, etc.), share it
        var shared = AssemblyLoadContext.Default.Assemblies.FirstOrDefault(a =>
            string.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase));
        if (shared != null)
            return shared;

        // 3) Otherwise resolve plugin-private deps (MathNet, etc.) via deps.json
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        if (path != null)
            return LoadFromAssemblyPath(path);

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path != null ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
    }
}