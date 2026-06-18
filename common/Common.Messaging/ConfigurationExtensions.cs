using System.Reflection;
using Common.Utils.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace QuantInfra.Common.Messaging
{
	public static class ConfigurationExtensions
	{
		public static IServiceCollection AddSingleAssemblyTypeResolver(this IServiceCollection sc, string assemblyName) =>
			sc.AddSingleton<ITypeResolver>(sc => new SingleAssemblyTypeResolver(Assembly.Load(assemblyName)));
	}
}

