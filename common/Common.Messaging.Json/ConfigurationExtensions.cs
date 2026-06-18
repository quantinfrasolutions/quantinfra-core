using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common.Utils.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NodaTime.Serialization.SystemTextJson;
using QuantInfra.Common.Messaging.Json.Messages.DealerRouterWithReplay;
using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;

namespace QuantInfra.Common.Messaging.Json
{
	public static class ConfigurationExtensions
	{
		public static IServiceCollection AddJsonMessages(this IServiceCollection sc, string? key = null, string? jsonSerializerSettingsKey = null)
		{
			return string.IsNullOrEmpty(key)
				? sc.AddSingleton<IMessageFactory>(sp => sp.ResolveJsonMessageFactory(jsonSerializerSettingsKey))
				: sc.AddKeyedSingleton<IMessageFactory>(key, (sp, _) => sp.ResolveJsonMessageFactory(jsonSerializerSettingsKey));
		}

		public static IServiceCollection AddJsonDealerRouterMessageFactory(this IServiceCollection sc,
			Func<IServiceProvider, string> resolveSenderCompId,
			string? jsonMessageFactoryKey = null
		) => sc
			.AddSingleton<IDealerRouterMessageFactory>(sp => new DealerRouterMessageFactory(
				resolveSenderCompId(sp),
				sp.ResolveJsonMessageFactory(jsonMessageFactoryKey)
			));

		private static JsonMessageFactory ResolveJsonMessageFactory(this IServiceProvider sp, string? jsonSerializerSettingsKey = null) => 
			new(
			sp.GetRequiredService<ITypeResolver>(),
				string.IsNullOrEmpty(jsonSerializerSettingsKey)
					? sp.GetRequiredService<JsonSerializerOptions>()
					: sp.GetRequiredKeyedService<JsonSerializerOptions>(jsonSerializerSettingsKey)
			);

		public static IServiceCollection AddDefaultJsonSerializerSettings(this IServiceCollection sc, string? key = null) =>
			string.IsNullOrEmpty(key)
				? sc.AddSingleton<JsonSerializerOptions>(JsonSerializerOptions)
				: sc.AddKeyedSingleton<JsonSerializerOptions>(key, JsonSerializerOptions);

		private static Lazy<JsonSerializerOptions> _jsonSerializerOptions = new(() =>
		{
			var options = new JsonSerializerOptions
			{
				WriteIndented = false,
				ReferenceHandler = ReferenceHandler.IgnoreCycles,
				PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
				UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode
			};
			options.ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb);
			options.Converters.Add(new JsonStringEnumConverter());
			options.Converters.Add(new JsonReadOnlyDictionaryConverter());
			
			return options;
		});
		
		public static JsonSerializerOptions JsonSerializerOptions => _jsonSerializerOptions.Value;
	}
}

