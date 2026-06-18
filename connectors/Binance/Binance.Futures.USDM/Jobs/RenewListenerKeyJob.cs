using Microsoft.Extensions.Logging;
using QuantInfra.Connectors.Binance.Futures.Usdm;
using Quartz;

namespace Binance.Futures.USDM.Jobs;

public class RenewListenerKeyJob : IJob
{
    public const string AccountIdKey = "AccountId";
    public const string RestClientKey = "RestClient";
    
    private readonly ILogger<RenewListenerKeyJob> _logger;

    public RenewListenerKeyJob(ILogger<RenewListenerKeyJob> logger)
    {
        _logger = logger;
    }
    
    public Task Execute(IJobExecutionContext context)
    {
        var accountId = context.JobDetail.JobDataMap.Get(AccountIdKey);
        _logger.LogInformation($"Renewing listener key for account {accountId}");
        var restClient = (RestClient)context.JobDetail.JobDataMap.Get(RestClientKey)!;
        return restClient.GetListenKey();
    }
}