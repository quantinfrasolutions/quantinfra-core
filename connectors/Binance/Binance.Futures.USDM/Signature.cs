using System.Security.Cryptography;
using System.Text;

namespace Binance.Futures.USDM;

public static class Signature
{
    public static string BuildSignaturePayload(this SortedDictionary<string, object> @params) =>
        string.Join("&", @params.Select(kvp => $"{kvp.Key}={kvp.Value}"));
    
    public static string GetHmacSha256Signature(this string payload, string apiSecret)
    {
        var hmac = new HMACSHA256(Encoding.ASCII.GetBytes(apiSecret));
        var signature = hmac.ComputeHash(Encoding.ASCII.GetBytes(payload));
        StringBuilder stringBuilder = new StringBuilder();
        foreach(byte b in signature) stringBuilder.AppendFormat("{0:x2}", b);
        string hashString = stringBuilder.ToString();
        return hashString;
    }
}