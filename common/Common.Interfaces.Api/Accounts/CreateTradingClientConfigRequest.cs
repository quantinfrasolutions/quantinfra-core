using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Common.Interfaces.Api.Accounts;

public class CreateTradingClientConfigRequest
{
    public CreateTradingClientConfigRequest()
    {
    }

    public CreateTradingClientConfigRequest(TradingClientConfig config)
    {
        AccountId = config.AccountId;
        ExecutionServiceName = config.ExecutionServiceName;
        ExternalAccountId = config.ExternalAccountId;
        TradingClientClassName = config.TradingClientClassName;
        TradingClientParamsSerialized = config.TradingClientParamsSerialized;
        WritePerformanceMetrics = config.WritePerformanceMetrics;
    }

    public int AccountId { get; set; }
    public string ExecutionServiceName { get; set; }
    public string? ExternalAccountId { get; set; }
    public string TradingClientClassName { get; set; }
    public string TradingClientParamsSerialized { get; set; }
    public string? TradingClientSecret { get; set; }
    public bool WritePerformanceMetrics { get; set; }

    public TradingClientConfig ToConfig() => new(AccountId, ExecutionServiceName, ExternalAccountId, TradingClientClassName, 
        TradingClientParamsSerialized, TradingClientSecret, WritePerformanceMetrics);
}