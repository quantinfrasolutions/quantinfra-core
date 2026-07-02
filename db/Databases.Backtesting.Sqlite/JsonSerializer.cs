using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace QuantInfra.Databases.Backtesting.Sqlite;

internal sealed class JsonValueConverter<T>() : ValueConverter<T, string>(
    v => JsonSerializer.Serialize(v, JsonSerializerOptions),
    v => JsonSerializer.Deserialize<T>(v, JsonSerializerOptions)!)
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