using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace QuantInfra.Backtesting.FileResultsRepository;

public class JsonOptions
{
    private static readonly Lazy<JsonSerializerOptions> _jsonSerializerOptions = new(() =>
    {
        var options = new JsonSerializerOptions()
        {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals | JsonNumberHandling.AllowReadingFromString,
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode,
            PropertyNameCaseInsensitive = true,
        };
        options.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    });
    public static JsonSerializerOptions JsonSerializerOptions => _jsonSerializerOptions.Value;
}