using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Databases.Main.Models.Events;
using QuantInfra.Databases.Main.Models.History;
using QuantInfra.Databases.Main.Models.Projections;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Events.Accounts.AccountsService.Projections;
using QuantInfra.Domain.Events.Accounts.Management;
using QuantInfra.Domain.Events.Persistence;
using QuantInfra.Domain.Events.Strategies.AccountsService;
using QuantInfra.Domain.Events.Strategies.Management;

namespace QuantInfra.Databases.Main.DAL;

public class EventPersister(MainContext context) : IEventPersister
{
    public void RecordProjection(UnrealizedPnLAccruedEvt evt)
    {
        
    }

    public void RecordProjection(string asName, SharePriceHistoryProjectionEvt evt)
    {
        context.SharePriceHistory.Add(evt.SharePrice);
        var entry = context.Entry(evt.SharePrice); 
        entry.Property("account_service_name").CurrentValue = asName;
        entry.Property("event_id").CurrentValue = evt.EventId;
    }

    public void RecordProjection(RealizedPnLAccruedEvt evt)
    {
        
    }

    public void RecordProjection(string asName, PositionChangedEvt evt)
    {
        if (evt.PositionHistoryRecord is not null)
        {
            var model = new PositionHistoryModel(evt.PositionHistoryRecord, asName, evt.EventId, evt.Type);
            context.PositionsHistory.Add(model);
        }
    }

    public void RecordBalanceMarkedToMarketProjectionEvt(BalanceMarkedToMarketProjectionEvt evt)
    {
        
    }

    public void RecordEvent(string asName, NewUnmappedContractRegisteredEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version)
        {
            AccountId = evt.AccountId,
            Data = JsonSerializer.SerializeToDocument(new NewUnmappedContractRegisteredEvtData(evt.ExternalContractId), 
                PersistentEventStorage.JsonSerializerOptions),
        };
        context.Events.Add(evtRecord);
    }

    public void RecordEvent(string asName, AccountReconciliationStatusChangedEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version)
        {
            AccountId = evt.AccountId,
            Data = JsonSerializer.SerializeToDocument(new AccountReconciliationStatusChangedEvtData(evt.NeedsReconciliation, evt.Message)),
        };
        context.Events.Add(evtRecord);
    }

    public void RecordEvent(string asName, BrokerAccountNeedsOrdersReconciliationEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version)
        {
            AccountId = evt.AccountId,
        };
        context.Events.Add(evtRecord);
    }

    public void RecordEvent(string asName, BrokerAccountOrdersReconciledEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version)
        {
            AccountId = evt.AccountId,
        };
        context.Events.Add(evtRecord);
    }

    public void RecordEvent(string asName, BrokerAccountNeedsTradesReconciliationEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version)
        {
            AccountId = evt.AccountId,
        };
        context.Events.Add(evtRecord);
    }

    public void RecordEvent(string asName, BrokerAccountTradesReconciledEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version)
        {
            AccountId = evt.AccountId,
        };
        context.Events.Add(evtRecord);
    }

    public void RecordProjection(string asName, BalanceHistoryProjectionEvt evt)
    {
        
    }

    public void RecordEvent(string accountServiceName, TradeEvt evt)
    {
        var evtRecord = new Event(accountServiceName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version)
        {
            AccountId = evt.AccountId,
            TradeId = evt.Trade.TradeId,
            Data = JsonSerializer.SerializeToDocument<TradeEvtData>(new(evt.AssetId)), 
        };
        context.Events.Add(evtRecord);

        var model = new TradeModel(evt.Trade, accountServiceName);
        context.Trades.Add(model);
    }

    public void RecordEvent(string asName, NewOrderSingleExternalCreatedEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version)
        {
            AccountId = evt.AccountId,
            ExecId = evt.ExecutionReport.ExecId,
        };
        context.Events.Add(evtRecord);
    }

    public void RecordEvent(string asName, OrderCancelRequestExternalCreatedEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version)
        {
            AccountId = evt.AccountId,
            ExecId = evt.ExecutionReport.ExecId,
        };
        context.Events.Add(evtRecord);
    }
    
    public void RecordEvent(string asName, OrderReplaceRequestExternalCreatedEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version)
        {
            AccountId = evt.AccountId,
            ExecId = evt.ExecutionReport.ExecId,
        };
        context.Events.Add(evtRecord);
    }

    public void RecordEvent(string asName, NewTradeInDeadLetterQueueEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version) { AccountId = evt.AccountId };
        context.Events.Add(evtRecord);

        var model = new ExternalTradeModel(evt.Trade, asName, evt.EventId);
        context.ExternalTrades.Add(model);
    }

    public void RecordEvent(string asName, SharePriceUpdatedEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version) { AccountId = evt.AccountId };
        context.Events.Add(evtRecord);

        context.SharePriceUpdates.Add(new()
        {
            AccountId = evt.AccountId,
            AccountServiceName = asName,
            EventId = evt.EventId,
            Equity = evt.Equity,
            SharePrice = evt.SharePrice,
            DailyReturn = evt.DailyReturn,
        });
    }

    public void RecordEvent(string asName, ShareCountUpdatedEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version) { AccountId = evt.AccountId };
        context.Events.Add(evtRecord);
        
        context.ShareCountUpdates.Add(new()
        {
            AccountId = evt.AccountId,
            AccountServiceName = asName,
            BalanceOperationId = evt.BalanceOperationId,
            Change = evt.Change,
            EventId = evt.EventId,
        });
    }

    public void RecordEvent(string asName, OrderCancelRejectEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version)
        {
            AccountId = evt.AccountId,
            Data = JsonSerializer.SerializeToDocument(new OrderCancelReplaceRejectEvtData(evt.Ocr.RejectReason, evt.Ocr.RejectText),
                PersistentEventStorage.JsonSerializerOptions),
        };
        context.Events.Add(evtRecord);
    }

    public void RecordEvent(string asName, ExecutionReportEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version)
        {
            AccountId = evt.AccountId,
            ExecId = evt.ExecutionReport.ExecId,
        };
        context.Events.Add(evtRecord);

        if (evt.ExecutionReport.AccountId != evt.AccountId) return;
        var model = new ExecutionReportModel(evt.ExecutionReport);
        context.OrdersHistory.Add(model);
    }
    
    public void RecordEvent(string asName, ExternalExecutionReportEvt evt)
    {
        // ExecutionReport is saved by RecordExecutionReportEvent called from SSA
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version)
        {
            AccountId = evt.AccountId,
            ExecId = evt.ExecutionReport.ExecId,
            Data = JsonSerializer.SerializeToDocument(new ExternalExecutionReportEvtData(evt.BrokerType, evt.ExternalContractId),
                PersistentEventStorage.JsonSerializerOptions),
        };
        context.Events.Add(evtRecord);
    }

    public void RecordEvent(string asName, TradingClientConfigurationChangedEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, 0)
        {
            AccountId = evt.AccountId,
            Data = JsonSerializer.SerializeToDocument(evt.Config, PersistentEventStorage.JsonSerializerOptions),
        };
        context.Events.Add(evtRecord);
    }

    public void RecordEvent(string asName, BalanceOperationProcessedEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version)
        {
            AccountId = evt.AccountId,
            BalanceOperationId = evt.BalanceOperation.BalanceOperationId,
        };
        context.Events.Add(evtRecord);
        
        context.BalanceOperations.Add(evt.BalanceOperation);
    }

    public void RecordEvent(string asName, AccountCreatedEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, 0) { AccountId = evt.AccountId };
        context.Events.Add(evtRecord);
    }

    public void RecordEvent(string asName, SubaccountAssignedEvt sa)
    {
        var evtRecord = new Event(asName, sa.EventId, sa.GetType().FullName!, sa.Timestamp, 0)
        {
            AccountId = sa.AccountId, 
            SubaccountId = sa.Subaccount.SubaccountHistoryId,
        };
        context.Events.Add(evtRecord);
    }

    public void RecordEvent(string accountServiceName, AccountEndOfDayEvt evt)
    {
        var evtRecord = new Event(accountServiceName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version) { AccountId = evt.AccountId };
        context.Events.Add(evtRecord);

        foreach (var pv in evt.PositionValues)
        {
            context.EndOfDayPositions.Add(pv.Value);
            context.Entry(pv.Value).Property("event_id").CurrentValue = evt.EventId;
            context.Entry(pv.Value).Property("account_service_name").CurrentValue = accountServiceName;
        }

        foreach (var bv in evt.BalanceValues)
        {
            context.EndOfDayBalances.Add(new(accountServiceName, evt.EventId, bv.Value));
        }
    }
    
    public void RecordEvent(string asName, StrategyCreatedEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, 0) { StrategyId = evt.StrategyId };
        context.Events.Add(evtRecord);
    }
    
    public void RecordEvent(string asName, StrategyLastCalculationTsUpdatedEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version)
        {
            StrategyId = evt.StrategyId,
            Data = JsonSerializer.SerializeToDocument(new StrategyLastCalculationTsEvtData(evt.Ts), 
                PersistentEventStorage.JsonSerializerOptions),
        };
        context.Events.Add(evtRecord);
    }

    public void RecordEvent(string asName, StrategyInternalStateUpdatedEvt evt)
    {
        var evtRecord = new Event(asName, evt.EventId, evt.GetType().FullName!, evt.Timestamp, evt.Version)
        {
            StrategyId = evt.StrategyId,
            Data = JsonDocument.Parse(System.Text.Encoding.UTF8.GetBytes(evt.InternalStateJson)),
        };
        context.Events.Add(evtRecord);
    }

    bool _isDisposed;
    public void Dispose()
    {
        if (_isDisposed) return;
        context.SaveChanges();
        context.Dispose();
        _isDisposed = true;
        Scope.Dispose();
    }
    
    internal IServiceScope Scope { get; set; }
}

public class EventPersisterFactory(IServiceProvider sp) : IEventPersisterFactory
{
    public IEventPersister Create()
    {
        var scope = sp.CreateScope();
        var persister = scope.ServiceProvider.GetRequiredService<EventPersister>();
        persister.Scope = scope;
        return persister;
    }

    public async Task<long> GetLastSavedEventIdAsync(string accountServiceName)
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return (await context.Events
            .Where(e => e.AccountServiceName == accountServiceName)
            .AsNoTracking()
            .OrderByDescending(e => e.EventId)
            .FirstOrDefaultAsync())?.EventId ?? 0;
    }
}