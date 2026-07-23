using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Databases.Main.Models.Events;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Events.Accounts.Management;
using QuantInfra.Domain.Events.Strategies.AccountsService;
using QuantInfra.Domain.Events.Strategies.Management;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Sdk.Trading.ExternalAccounts;
using QuantInfra.Sdk.Trading.Orders;
using QuantInfra.Services.AccountsCore.State;

namespace QuantInfra.Databases.Main.DAL;

public class PersistentEventStorage(IServiceProvider serviceProvider) : IPersistentEventStorage<AccountServiceState>
{
    public Task<AccountServiceState?> GetLatestStateSnapshot(string instanceName)
    {
        // TODO
        return Task.FromResult<AccountServiceState?>(null);
    }

    public async Task<IReadOnlyList<IEvent>> GetEventsSinceLastSnapshot(string instanceName, int limit, int offset)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();

        return (await GetEventsQuery(
            context.Events
                .Where(e => e.AccountServiceName == instanceName)
                .OrderBy(e => e.EventId)
                .Skip(offset)
                .Take(limit)
            )
            .AsNoTracking()
            .ToListAsync())
            .Select(ConstructEvent)
            .ToList();
    }

    public IEvent? GetEvent(string accountServiceName, long eventId)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<MainContext>();

        return GetEventsQuery(context.Events.Where(e => e.AccountServiceName == accountServiceName && e.EventId == eventId))
            .AsNoTracking()
            .Select(ConstructEvent)
            .SingleOrDefault();
    }

    private IQueryable<Event> GetEventsQuery(IQueryable<Event> source) => source
        .Include(e => e.Account)
            .ThenInclude(a => a.Currency)
        .Include(e => e.Account)
            .ThenInclude(a => a.Broker)
        .Include(e => e.Subaccount)
        .Include(e => e.Strategy)
        .Include(e => e.BalanceOperation)
        .Include(e => e.EndOfDayBalances)
            .ThenInclude(b => b.Currency).ThenInclude(c => c.Asset)
        .Include(e => e.EndOfDayPositions)
        .Include(e => e.ExecutionReport)
            .ThenInclude(er => er.Contract)
        .Include(e => e.ShareCountUpdate)
        .Include(e => e.SharePriceUpdate)
        .Include(e => e.ExecutionReport)
        .Include(e => e.Trade)
            .ThenInclude(t => t.Contract)
                .ThenInclude(c => c.Template)
        .Include(e => e.Trade)
            .ThenInclude(t => t.PaymentCurrency)
        .Include(e => e.ExternalTrade);

    private IEvent ConstructEvent(Event e)
    {
        switch (e.EventType)
        {
            case "QuantInfra.Domain.Events.Accounts.AccountsService.Primary.AccountEndOfDayEvt":
                var eodData = e.Data!.Deserialize<AccountEndOfDayEvtData>(JsonSerializerOptions);
                return new AccountEndOfDayEvt(e.EventId, e.AccountId!.Value, e.Version,
                        e.EndOfDayPositions.ToDictionary(p => p.PositionId),
                        e.EndOfDayBalances.ToDictionary(b => b.Currency.CurrencyId, b => (BalanceValue)b),
                        true,
                        eodData.Options,
                        e.EndOfDayPositions.Select(p => p.Dt).DefaultIfEmpty(e.Timestamp).First(),
                        e.Timestamp);

            case "QuantInfra.Domain.Events.Accounts.AccountsService.Primary.BalanceOperationProcessedEvt":
                return new BalanceOperationProcessedEvt(e.EventId, e.AccountId!.Value, e.Version,
                    e.BalanceOperation!, e.Timestamp, null);

            case "QuantInfra.Domain.Events.Accounts.AccountsService.Primary.ExecutionReportEvt":
                return new ExecutionReportEvt(e.EventId, e.AccountId!.Value, e.Version,
                    e.Account!.AccountType,
                    e.ExecutionReport!, e.Timestamp);

            case "QuantInfra.Domain.Events.Accounts.AccountsService.Primary.ExternalExecutionReportEvt":
                var eerData = e.Data!.Deserialize<ExternalExecutionReportEvtData>(JsonSerializerOptions);
                return new ExternalExecutionReportEvt(e.EventId, e.AccountId!.Value, e.Version,
                    eerData.BrokerType, eerData.ExternalContractId, e.ExecutionReport!, e.Timestamp);

            case "QuantInfra.Domain.Events.Accounts.AccountsService.Primary.NewOrderSingleExternalCreatedEvt":
                return new NewOrderSingleExternalCreatedEvt(e.EventId, e.AccountId!.Value,
                    new NewOrderSingleExternal(e.ExecutionReport!,
                        e.ExecutionReport!.BrokerAccountId!.Value,
                        e.ExecutionReport!.Contract.ExternalContractId!,
                        e.ExecutionReport.OrderId.ToString()),
                    e.ExecutionReport!, e.Account!.Broker!.BrokerType, e.Version, e.Timestamp
                );

            case "QuantInfra.Domain.Events.Accounts.AccountsService.Primary.NewTradeInDeadLetterQueueEvt":
                return new NewTradeInDeadLetterQueueEvt(e.EventId, e.AccountId!.Value, e.ExternalTrade!,
                    e.Version, e.Timestamp);

            case "QuantInfra.Domain.Events.Accounts.AccountsService.Primary.NewUnmappedContractRegisteredEvt":
                var unmData = e.Data!.Deserialize<NewUnmappedContractRegisteredEvtData>(JsonSerializerOptions);
                return new NewUnmappedContractRegisteredEvt(e.EventId, e.AccountId!.Value,
                    unmData.ExternalContractId, unmData.ExternalAssetId, e.Version, e.Timestamp);

            case "QuantInfra.Domain.Events.Accounts.AccountsService.Primary.OrderCancelRejectEvt":
                var ocrrData = e.Data != null 
                    ? e.Data.Deserialize<OrderCancelReplaceRejectEvtData?>(JsonSerializerOptions)
                    : null;
                return new OrderCancelRejectEvt(e.AccountId!.Value, e.EventId, 
                    new(e.AccountId!.Value, null, string.Empty, ocrrData?.Reason ?? CxlRejReason.Other, ocrrData?.RejectText),
                    e.Timestamp,
                    e.Version);

            case "QuantInfra.Domain.Events.Accounts.AccountsService.Primary.OrderCancelRequestExternalCreatedEvt":
                return new OrderCancelRequestExternalCreatedEvt(e.EventId, e.AccountId!.Value,
                    new OrderCancelRequestExternal(e.ExecutionReport!.OrderId,
                        e.ExecutionReport!.BrokerAccountId!.Value,
                        e.ExecutionReport!.Contract.ExternalContractId!, e.ExecutionReport.ExternalId),
                    e.ExecutionReport!, e.Version, e.Timestamp
                );
            
            case "QuantInfra.Domain.Events.Accounts.AccountsService.Primary.OrderReplaceRequestExternalCreatedEvt":
                return new OrderReplaceRequestExternalCreatedEvt(e.EventId, e.AccountId!.Value,
                    new OrderReplaceRequestExternal(
                        new() { AccountId = e.AccountId!.Value, OrderId = e.ExecutionReport!.OrderId, 
                            Price = e.ExecutionReport.Price, OrderQty = e.ExecutionReport.OrderQty, StopPx = e.ExecutionReport.StopPx },
                        e.ExecutionReport!.Contract.ExternalContractId!, e.ExecutionReport!.ExternalId!, e.ExecutionReport!.OrdType),
                    e.ExecutionReport!, e.Version, e.Timestamp
                );

            case "QuantInfra.Domain.Events.Accounts.AccountsService.Primary.ShareCountUpdatedEvt":
                return new ShareCountUpdatedEvt(e.EventId, e.AccountId!.Value, e.ShareCountUpdate!.Change,
                    e.ShareCountUpdate.BalanceOperationId, e.Version, e.Timestamp);

            case "QuantInfra.Domain.Events.Accounts.AccountsService.Primary.SharePriceUpdatedEvt":
                return new SharePriceUpdatedEvt(e.EventId, e.AccountId!.Value, e.SharePriceUpdate!.Equity,
                    e.SharePriceUpdate.SharePrice, e.SharePriceUpdate!.DailyReturn, e.Version, e.Timestamp, e.Timestamp);

            case "QuantInfra.Domain.Events.Accounts.AccountsService.Primary.TradeEvt":
                var tradeData = e.Data!.Deserialize<TradeEvtData>(JsonSerializerOptions)!;                
                return new TradeEvt(e.EventId, e.AccountId!.Value, e.Trade!, e.Version, e.Timestamp,
                    tradeData.AssetId, e.Trade!.PaymentCurrency.Decimals,
                    e.Trade.Contract.Template.SecurityType, e.Account!.Currency.Decimals,
                    tradeData.Options);
            
            case "QuantInfra.Domain.Events.Accounts.AccountsService.Primary.AccountReconciliationStatusChangedEvt":
                var reconData = e.Data!.Deserialize<AccountReconciliationStatusChangedEvtData>(JsonSerializerOptions);
                return new AccountReconciliationStatusChangedEvt(e.EventId, e.AccountId!.Value, e.Version,
                    reconData.NeedsReconciliation,
                    reconData.Message, e.Timestamp);
            
            case "QuantInfra.Domain.Events.Accounts.AccountsService.Primary.BrokerAccountNeedsOrdersReconciliationEvt":
                return new BrokerAccountNeedsOrdersReconciliationEvt(e.EventId, e.AccountId!.Value, e.Version!, e.Timestamp);
            
            case "QuantInfra.Domain.Events.Accounts.AccountsService.Primary.BrokerAccountOrdersReconciledEvt":
                return new BrokerAccountOrdersReconciledEvt(e.EventId, e.AccountId!.Value, e.Version!, e.Timestamp);
            
            case "QuantInfra.Domain.Events.Accounts.AccountsService.Primary.BrokerAccountNeedsTradesReconciliationEvt":
                // HACK: last dts are not populated here, because they are not applied to the state and are used only by ES
                return new BrokerAccountNeedsTradesReconciliationEvt(e.EventId, e.AccountId!.Value,
                    new Dictionary<string, Instant>(),
                    Instant.MinValue,
                    e.Version!, e.Timestamp
                );
            
            case "QuantInfra.Domain.Events.Accounts.AccountsService.Primary.BrokerAccountTradesReconciledEvt":
                return new BrokerAccountTradesReconciledEvt(e.EventId, e.AccountId!.Value, e.Version!, e.Timestamp);

            case "QuantInfra.Domain.Events.Accounts.Management.AccountCreatedEvt":
                return new AccountCreatedEvt(e.EventId, e.AccountId!.Value, e.Account!, e.Timestamp);

            case "QuantInfra.Domain.Events.Accounts.Management.SubaccountAssignedEvt":
                return new SubaccountAssignedEvt(e.EventId, e.AccountServiceName, e.AccountId!.Value,
                    e.Subaccount!, e.Timestamp);
            
            case "QuantInfra.Domain.Events.Accounts.Management.TradingClientConfigurationChangedEvt":
                var tc = e.Data!.Deserialize<TradingClientConfig>(JsonSerializerOptions);
                return new TradingClientConfigurationChangedEvt(e.EventId, e.AccountId!.Value, tc, e.Timestamp);

            case "QuantInfra.Domain.Events.Strategies.AccountsService.StrategyInternalStateUpdatedEvt":
                var stData = JsonSerializer.Serialize(e.Data, StrategyConfig.JsonSerializerOptions);
                return new StrategyInternalStateUpdatedEvt(e.EventId, e.StrategyId!.Value,
                    stData, e.Version, e.Timestamp);

            case "QuantInfra.Domain.Events.Strategies.AccountsService.StrategyLastCalculationTsUpdatedEvt":
                var strData = e.Data!.Deserialize<StrategyLastCalculationTsEvtData>(JsonSerializerOptions);
                return new StrategyLastCalculationTsUpdatedEvt(e.EventId, e.StrategyId!.Value,
                    strData.Ts, e.Version, e.Timestamp);

            case "QuantInfra.Domain.Events.Strategies.Management.StrategyCreatedEvt":
                return new StrategyCreatedEvt(e.EventId, e.StrategyId!.Value, e.Strategy!, e.Account!,
                    e.Timestamp) as IEvent;
            
            default: throw new NotSupportedException($"Event type {e.EventType} is not supported");
        }
    }

    private static Lazy<JsonSerializerOptions> _jsonSerializerOptions = new(() =>
    {
        var jsonSerializerOptions = new JsonSerializerOptions()
        {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals |
                             JsonNumberHandling.AllowReadingFromString,
            WriteIndented = false,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode,
            PropertyNameCaseInsensitive = true,
        };

        jsonSerializerOptions.ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb);
        jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        return jsonSerializerOptions;
    });
    
    public static JsonSerializerOptions JsonSerializerOptions => _jsonSerializerOptions.Value;
}