using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Net.Http.Headers;
using NodaTime.Serialization.SystemTextJson;
using QuantInfra.Common.Messaging.Json;

namespace QuantInfra.Api.Client
{
    public class ServiceWrapper
	{
		public ServiceWrapper(ServiceWrapperConfig config)
		{
			var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add(HeaderNames.AccessControlAllowOrigin, "*");
			httpClient.BaseAddress = new Uri(config.Endpoint);
			Client = new(httpClient);
        }

		public ApiClient Client { get; }


		internal static void ConfigureJsonOptions(JsonSerializerOptions options)
        {
            options.ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb);
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new JsonReadOnlyDictionaryConverter());
            options.NumberHandling = JsonNumberHandling.AllowReadingFromString |
                                     JsonNumberHandling.AllowNamedFloatingPointLiterals;
        }
    }
}

