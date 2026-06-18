using System.Text.Json.Serialization;
using System.Web;
using Binance.Futures.USDM;
using Microsoft.Extensions.Logging;

namespace QuantInfra.Binance.Futures.USDM.Client;

public partial class BinanceClient
{
    public string ApiSecret { get; set; }
    public ILogger Logger { get; set; }
    
    static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
    {
        settings.NumberHandling = JsonNumberHandling.AllowReadingFromString |
                                  JsonNumberHandling.AllowNamedFloatingPointLiterals;
        settings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    }

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url)
    {
        if (request.Headers.Contains("AUTH-REQUIRED"))
        {
            request.Headers.Remove("AUTH-REQUIRED");

            string signature = null;
            
            switch (request.Content)
            {
                case FormUrlEncodedContent form:
                    var contentString = form.ReadAsStringAsync().Result;
                    Logger.LogDebug($"query: {contentString}");
                    signature = contentString!.GetHmacSha256Signature(ApiSecret);
                    break;
            }

            
            // var uriBuilder = new UriBuilder(request.RequestUri!);
            var uri = new Uri(_httpClient.BaseAddress!, request.RequestUri!);
            var query = HttpUtility.ParseQueryString(uri.Query);

            if (request.Content != null && query.Count != 0)
            {
                throw new NotSupportedException("Passing parameters both in content and in query is not supported");
            }
            if (string.IsNullOrEmpty(signature))
            {
                // Logger.LogDebug($"query: {query}");
                signature = query!.ToString()!.GetHmacSha256Signature(ApiSecret);
            }
            query["signature"] = signature;
            request.RequestUri = new Uri($"{uri.LocalPath}?{query.ToString()}", UriKind.Relative);
        }
    }
}