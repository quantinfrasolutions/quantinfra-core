using Microsoft.Extensions.Logging;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Events.Strategies.Management;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Domain.StrategyRecordsStateManager.ExternalEventHandlers;

class StrategyUpdatesHandlerAccountsService : 
    IExternalEventHandler<StrategyCreatedEvt>,
    IExternalEventHandler<StrategyStatusChangedEvt>
{
    private readonly AccountsServiceStateManager _accountsServiceStateManager;
    private readonly ILogger _logger;
    private readonly IQueryBus _queryBus;

    public StrategyUpdatesHandlerAccountsService(AccountsServiceStateManager accountsServiceStateManager, ILogger<StrategyUpdatesHandler> logger, IQueryBus queryBus)
    {
        _accountsServiceStateManager = accountsServiceStateManager;
        _logger = logger;
        _queryBus = queryBus;
    }
    
    public void Apply(StrategyCreatedEvt e)
    {
        var account = _queryBus.Query<GetAccount, AccountRecordV6?>(new(e.Strategy.AccountId));
        if (account != null)
        {
            _logger.LogInformation($"Adding strategy {e.Strategy.StrategyId}");
            _accountsServiceStateManager.SetStrategyRecord(e.Strategy);
        }
    }

    public void Apply(StrategyStatusChangedEvt e)
    {
        if (_accountsServiceStateManager.StrategyRecords.TryGetValue(e.StrategyId, out var strategy))
        {
            _logger.LogInformation($"Update strategy {strategy.StrategyId} status to {e.Status}");
            strategy.Status = e.Status;
        }
    }
}