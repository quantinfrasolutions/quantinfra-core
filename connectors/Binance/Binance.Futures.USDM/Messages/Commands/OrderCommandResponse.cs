using System.Text.Json;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Messages.Commands;

public readonly record struct OrderCommandResponse(string RequestId, int Status, int? ErrorCode, string? ErrorMessage)
{
    public bool IsSuccess => Status is >= 200 and < 300 && !ErrorCode.HasValue;

    public static bool TryParse(ReadOnlyMemory<byte> json, out OrderCommandResponse response)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("id", out var idElement))
        {
            response = default;
            return false;
        }

        var requestId = idElement.ValueKind == JsonValueKind.String
            ? idElement.GetString()
            : idElement.GetRawText();
        if (string.IsNullOrEmpty(requestId))
        {
            response = default;
            return false;
        }

        var status = root.TryGetProperty("status", out var statusElement) && statusElement.TryGetInt32(out var parsedStatus)
            ? parsedStatus
            : 0;
        int? errorCode = null;
        string? errorMessage = null;
        if (root.TryGetProperty("error", out var errorElement))
        {
            if (errorElement.TryGetProperty("code", out var codeElement) && codeElement.TryGetInt32(out var parsedCode))
                errorCode = parsedCode;
            if (errorElement.TryGetProperty("msg", out var messageElement))
                errorMessage = messageElement.GetString();
        }

        response = new OrderCommandResponse(requestId, status, errorCode, errorMessage);
        return true;
    }
}
