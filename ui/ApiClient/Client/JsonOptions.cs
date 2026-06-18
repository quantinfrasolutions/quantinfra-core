using System.Text.Json;

namespace QuantInfra.Api.Client;

public partial class ApiClient
{
    static partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
    {
        ServiceWrapper.ConfigureJsonOptions(settings);
    }
}