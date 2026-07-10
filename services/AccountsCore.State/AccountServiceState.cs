using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;
using QuantInfra.Common.ServiceBase.WAL;
using QuantInfra.Common.Utils.Collections;
using QuantInfra.Domain.Account.Execution.State;
using QuantInfra.Domain.AccountRecordsStateManager;
using QuantInfra.Domain.Accounts.Base;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.MarketData;
using QuantInfra.Domain.StaticData;
using QuantInfra.Domain.Strategies;
using QuantInfra.Domain.StrategyRecordsStateManager;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.AccountStates;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Sdk.Trading;
using Contract = QuantInfra.Sdk.StaticData.Contract;
using Strategy = QuantInfra.Sdk.Strategies.Strategy;

[assembly:InternalsVisibleTo("QuantInfra.Services.AccountsCore")]

namespace QuantInfra.Services.AccountsCore.State;

public class AccountServiceState :
    IState<AccountServiceState>,
    IEventIdProvider, 
    IBalanceOperationIdProvider,
    IOrderIdProvider,
    IExecIdProvider,
    ITradeIdProvider,
    IReceiverStateProvider,
    ILastContractPricesStore,
    IStaticDataRepositoryStateStore,
    IAccountRecordsStore,
    IStrategyRecordsStore
{
    public AccountServiceState()
    { }
    
    // BrokerAccountStatesReadonly
    // StrategyStatesReadonly
    // LastPrices
    // Contracts
    // AssetsByExternalId
    // Currencies
    // ConversionPaths
    // Brokers
    // AccountRecords
    // Subaccounts
    // StrategyRecords
    // EventId
    // LastFinalizedEventId
    // LastFinalizedTimestamp
    // BalanceOperationId
    // OrderId
    // ExecId
    // TradeId
    // ReceiverState
    
    [JsonConstructor]
    public AccountServiceState(
        IEnumerable<AccountStateReadonly> accountStatesReadonly,
        IEnumerable<BrokerAccountStateReadonly> brokerAccountStatesReadonly,
        // IEnumerable<ExecutableSubaccountStateReadonly> esaStatesReadonly,
        IEnumerable<StrategyStateReadonly> strategyStatesReadonly,
        Dictionary<int, AccountRecordV6> accountRecords,
        Dictionary<int, Dictionary<SubaccountType, List<Subaccount>>> subaccounts,
        Dictionary<int, Strategy> strategyRecords,
        Dictionary<int, LastPrice> lastPrices,
        Dictionary<int, Contract?> contracts,
        Dictionary<int, Asset?> assets,
        Dictionary<int, Dictionary<string, Asset?>> assetsByExternalId,
        Dictionary<int, Currency?> currencies,
        Dictionary<int, Dictionary<int, IReadOnlyCollection<FxConversionStep>>> conversionPaths,
        Dictionary<int, Broker?> brokers,
        long eventId,
        int balanceOperationId,
        long lastFinalizedEventId,
        long lastFinalizedTimestamp,
        long orderId,
        long execId,
        long tradeId,
        ReceiverState receiverState
    )
    {
        UninitializedAccountStates = accountStatesReadonly.ToList();
        UninitializedBrokerAccountStates = brokerAccountStatesReadonly.ToList();
        // UninitializedEsaStates = esaStatesReadonly.ToList();
        UninitializedStrategyStates = strategyStatesReadonly.ToList();
        Contracts = contracts;
        Assets = assets ?? new();
        AssetsByExternalId = assetsByExternalId ?? new();
        ContractsByExternalId = contracts.Values
            .Where(c => !string.IsNullOrEmpty(c?.ExternalContractId))
            .GroupBy(c => c!.Template.Broker.BrokerId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(
                    c => c.ExternalContractId!,
                    c => c
                )
            );
        Currencies = currencies;
        ConversionPaths = conversionPaths;
        Brokers = brokers;
        EventId = eventId;
        BalanceOperationId = balanceOperationId;
        LastFinalizedEventId = lastFinalizedEventId;
        LastFinalizedTimestamp = lastFinalizedTimestamp;
        OrderId = orderId;
        ExecId = execId;
        TradeId = tradeId;
        ReceiverState = receiverState;
        AccountRecords = accountRecords;
        StrategyRecords = strategyRecords;
        Subaccounts = subaccounts ?? new();
        ReverseSubaccounts = StateManager.GetReverseSubaccounts(Subaccounts.Values.SelectMany(kv => kv.Values.SelectMany(i => i)));
    }
    
    
    
    [JsonIgnore] public Dictionary<int, AccountBaseState> AccountStates { get; } = new();
    [JsonIgnore] internal IReadOnlyCollection<AccountStateReadonly> UninitializedAccountStates { get; private set; } 
        = new List<AccountStateReadonly>();
    [JsonIgnore] internal IReadOnlyCollection<BrokerAccountStateReadonly> UninitializedBrokerAccountStates { get; private set; } 
        = new List<BrokerAccountStateReadonly>();
    // [JsonIgnore] internal IReadOnlyCollection<ExecutableSubaccountStateReadonly> UninitializedEsaStates { get; private set; } 
    //     = new List<ExecutableSubaccountStateReadonly>();
    
    public IEnumerable<AccountStateReadonly> AccountStatesReadonly
    {
        get => AccountStates.Values
            .Where(a => a is not BrokerAccountState/* && a is not ExecutableSubaccountState*/)
            .Select(a => a.ToAccountStateReadonly()).ToList();
        set => UninitializedAccountStates = value.ToList();
    }

    public IEnumerable<BrokerAccountStateReadonly> BrokerAccountStatesReadonly
    {
        get => AccountStates.Values
            .Where(a => a is BrokerAccountState)
            .Select(a => ((BrokerAccountState)a).ToAccountStateReadonly()).ToList();
        set => UninitializedBrokerAccountStates = value.ToList();
    }
    
    // public IEnumerable<ExecutableSubaccountStateReadonly> EsaStatesReadonly
    // {
    //     get => AccountStates.Values
    //         .Where(a => a is ExecutableSubaccountState)
    //         .Select(a => ((ExecutableSubaccountState)a).ToAccountStateReadonly()).ToList();
    //     set => UninitializedEsaStates = value.ToList();
    // }

    [JsonIgnore] public Dictionary<int, StrategyState> StrategyStates { get; private set; } = new();
    [JsonIgnore] internal IEnumerable<StrategyStateReadonly> UninitializedStrategyStates { get; private set; } = new List<StrategyStateReadonly>();
    public IEnumerable<StrategyStateReadonly> StrategyStatesReadonly
    {
        get => StrategyStates.Values.Select(a => a.ToStrategyStateReadonly()).ToList();
        set => UninitializedStrategyStates = value.ToList();
    }
    
    public Dictionary<int, LastPrice> LastPrices { get; private set; } = new();
    public Dictionary<int, Contract?> Contracts { get; private set; } = new();
    [JsonIgnore] public Dictionary<int, Dictionary<string, Contract?>> ContractsByExternalId { get; private set; } = new();
    public Dictionary<int, Asset?> Assets { get; private set; } = new();
    public Dictionary<int, Dictionary<string, Asset?>> AssetsByExternalId { get; private set; } = new();
    public Dictionary<int, Currency?> Currencies { get; private set; } = new();
    public Dictionary<int, Dictionary<int, IReadOnlyCollection<FxConversionStep>>> ConversionPaths { get; private set; } = new();
    public Dictionary<int, Broker?> Brokers { get; private set; } = new();
    public Dictionary<int, AccountRecordV6> AccountRecords { get; set; } = new();
    public Dictionary<int, Dictionary<SubaccountType, List<Subaccount>>> Subaccounts { get; set; } = new();
    [JsonIgnore] public Dictionary<int, Dictionary<SubaccountType, List<int>>> ReverseSubaccounts { get; set; } = new();
    public Dictionary<int, QuantInfra.Sdk.Strategies.Strategy> StrategyRecords { get; set; } = new();

    public const long InitialEventId = 1000000000;
    public long EventId { get; private set; } = InitialEventId;
    public long GetNextEventId() => EventId++;
    public void UpdateEventId(long eventId) => EventId = eventId + 1;

    public long LastFinalizedEventId { get; private set; } = 1000000000;
    public long LastFinalizedTimestamp { get; private set; } = Instant.MinValue.ToUnixTimeMilliseconds();

    public void UpdateLastSentEventId(long eventId, long timestamp)
    {
        LastFinalizedEventId = eventId;
        LastFinalizedTimestamp = timestamp;
    }

    public void SetLastFinalizedEventId(long eventId)
    {
        LastFinalizedEventId = eventId;
    }
    
    public void Initialize(AccountServiceState state)
    {
        UninitializedAccountStates = state.UninitializedAccountStates.ToList();
        UninitializedBrokerAccountStates = state.UninitializedBrokerAccountStates.ToList();
        // UninitializedEsaStates = state.UninitializedEsaStates.ToList();
        UninitializedStrategyStates = state.UninitializedStrategyStates.ToList();
        Contracts = state.Contracts.Copy();
        ContractsByExternalId = Contracts.Values
            .Where(c => !string.IsNullOrEmpty(c?.ExternalContractId))
            .GroupBy(c => c!.Template.Broker.BrokerId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(
                    c => c.ExternalContractId!,
                    c => c
                )
            );
        Assets = state.Assets.Copy();
        AssetsByExternalId = state.AssetsByExternalId.Copy();
        Currencies = state.Currencies.Copy();
        ConversionPaths = state.ConversionPaths.Copy();
        Brokers = state.Brokers.Copy();
        EventId = state.EventId;
        BalanceOperationId = state.BalanceOperationId;
        LastFinalizedEventId = state.LastFinalizedEventId;
        LastFinalizedTimestamp = state.LastFinalizedTimestamp;
        OrderId = state.OrderId;
        ExecId = state.ExecId;
        TradeId = state.TradeId;
        ReceiverState = state.ReceiverState;
        AccountRecords = state.AccountRecords.Copy();
        StrategyRecords = state.StrategyRecords.Copy();
        Subaccounts = state.Subaccounts.Copy();
        ReverseSubaccounts = StateManager.GetReverseSubaccounts(Subaccounts.Values.SelectMany(kv => kv.Values.SelectMany(i => i)));
    }
    
    public int BalanceOperationId { get; private set; } = 100000;
    public int GetNextBalanceOperationId() => BalanceOperationId++;
    public void UpdateBalanceOperationId(int balanceOperationId) => BalanceOperationId = balanceOperationId + 1;

    public long OrderId { get; private set; } = 100000000;
    public long GetNextOrderId() => OrderId++;
    public void UpdateOrderId(long orderId)
    {
        OrderId = Math.Max(OrderId, orderId + 1);
    }

    public long ExecId { get; private set; } = 1000000000;
    public long GetNextExecId() => ExecId++;
    public void UpdateExecId(long execId)
    {
        ExecId = execId + 1;
    }

    public long TradeId { get; private set; } = 1000000;
    public long GetNextTradeId() => TradeId++;
    public void UpdateTradeId(long tradeId)
    {
        TradeId = tradeId + 1;
    }

    public ReceiverState ReceiverState { get; private set; } = new();

    public ReceiverState GetReceiverState() => ReceiverState;

    public void UpdateState(string senderCompId, long sessionId, long sequenceNumber) =>
        ReceiverState.SetSession(senderCompId, sessionId, sequenceNumber);
    
    
    [JsonIgnore] public Instant? LastProcessedHeartbeatTs { get; set; }
    [JsonIgnore] public Instant? LastMarketDataEvtProcessingTs { get; set; }
}