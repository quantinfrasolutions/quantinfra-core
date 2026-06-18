using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.AccountStates;

namespace QuantInfra.Domain.Accounts.AccountStateClientManager;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureAccountStateClientManager(this IServiceCollection services, IConfiguration configuration,
        string sectionName = "accounts-state-manager") => services
        .Configure<Config>(conf => configuration.GetSection(sectionName).Bind(conf))
        .AddSingleton<Config>(sp => sp.GetRequiredService<IOptions<Config>>()?.Value ?? new());
    
    public static IServiceCollection AddAccountStateClientManagerHandlers(this IServiceCollection sc) => sc
        .AddSingleton<IExternalEventHandler<BalanceOperationProcessedEvt>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IExternalEventHandler<ExecutionReportEvt>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IExternalEventHandler<ExternalExecutionReportEvt>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IExternalEventHandler<TradeEvt>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IExternalEventHandler<OrderCancelRejectEvt>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IExternalEventHandler<AccountEndOfDayEvt>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IExternalEventHandler<ShareCountUpdatedEvt>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IExternalEventHandler<SharePriceUpdatedEvt>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IExternalEventHandler<NewOrderSingleExternalCreatedEvt>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IExternalEventHandler<OrderCancelRequestExternalCreatedEvt>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IExternalEventHandler<OrderReplaceRequestExternalCreatedEvt>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IExternalEventHandler<NewTradeInDeadLetterQueueEvt>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IExternalEventHandler<NewUnmappedContractRegisteredEvt>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IExternalEventHandler<AccountReconciliationStatusChangedEvt>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IExternalEventHandler<BrokerAccountNeedsOrdersReconciliationEvt>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IExternalEventHandler<BrokerAccountOrdersReconciledEvt>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IExternalEventHandler<BrokerAccountNeedsTradesReconciliationEvt>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IExternalEventHandler<BrokerAccountTradesReconciledEvt>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IAsyncQueryResponseHandler<GetAccountState, AccountStateReadonly?>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IAsyncQueryResponseHandler<GetBrokerAccountState, BrokerAccountStateReadonly?>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IQueryHandler<GetAccount, IAccountStateReadonly?>>(sp => sp.GetRequiredService<AccountsStateManager>())
        .AddSingleton<IQueryHandler<GetAccount, IBrokerAccountStateReadonly?>>(sp => sp.GetRequiredService<AccountsStateManager>());

    public static IServiceCollection AddAccountStateClientManager(this IServiceCollection sc) => sc
        .AddSingleton<AccountsStateManager>()
        .AddAccountStateClientManagerHandlers();
    
    public static IServiceCollection AddAccountsTradingApi(this IServiceCollection sc) => sc
        .AddSingleton<AccountsTradingApi>()
        .AddSingleton<AccountsStateManager>(sp => sp.GetRequiredService<AccountsTradingApi>())
        .AddAccountStateClientManagerHandlers()
        .AddSingleton<IQueryHandler<GetAccount, ITradingAccount?>>(sp => sp.GetRequiredService<AccountsTradingApi>());
}