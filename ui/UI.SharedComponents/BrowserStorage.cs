using System.Text.Json;
using Microsoft.JSInterop;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace UI.SharedComponents;

public sealed class BrowserStorage
{
    private readonly IJSRuntime _js;
    private readonly JsonSerializerOptions _jsonOptions;

    public BrowserStorage(IJSRuntime js)
    {
        _js = js;
        _jsonOptions = new(JsonSerializerDefaults.Web);
        _jsonOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await _js.InvokeAsync<string?>("localStorage.getItem", key);
        
        if (json is null) return default;

        try
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch
        {
            await RemoveAsync(key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        await _js.InvokeVoidAsync("localStorage.setItem", key, json);
    }

    public Task RemoveAsync(string key) =>
        _js.InvokeVoidAsync("localStorage.removeItem", key).AsTask();
}