using Common.Trading;
using NodaTime;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.AccountStates;
using QuantInfra.Sdk.Accounts.ExternalAccounts;
using QuantInfra.Sdk.Trading.ExternalAccounts;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Tests.Mocks;

public class MockBrokerAccount : IBrokerAccount
{
    public List<NewOrderSingle> Nos { get; } = new();
    public List<OrderCancelRequestExternal> Ocr { get; } = new();
    public List<ExecutionReport> ExternalOrders { get; set; } = new();
    public List<ExecutionReport> ExternalOcrs { get; set; } = new();

    public int AccountId { get; set; }


    public void OnExternalExecutionReport(ExternalExecutionReport externalER, Instant processingDt)
    {
        throw new NotImplementedException();
    }

    public void OnExternalOrderCancelReject(OrderCancelReject externalOcr, Instant processingDt)
    {
        throw new NotImplementedException();
    }

    public void OnExternalOrderCancelReject(ExternalOrderCancelReject externalOcr, Instant processingDt)
    {
        throw new NotImplementedException();
    }

    public void OnExternalTrade(ExternalTradeRecord externalTradeRecord, Instant processingDt)
    {
        throw new NotImplementedException();
    }

    public void OnExternalPositionReport(ExternalPositionReport positionReport, Instant processingDt)
    {
        throw new NotImplementedException();
    }

    public void OnExternalAccountOrdersSnapshot(ExternalAccountOrdersSnapshot snapshot, Instant processingDt)
    {
        throw new NotImplementedException();
    }

    public void OnExternalAccountTradesReport(ExternalAccountTradesReport externalAccountTradesReport, Instant processingDt)
    {
        throw new NotImplementedException();
    }

    public void OnExternalAccountPositionsSnapshot(AccountPositionsSnapshot snapshot, Instant processingDt)
    {
        throw new NotImplementedException();
    }

    public void OnExternalAccountBalancesSnapshot(AccountBalancesSnapshot snapshot, Instant processingDt)
    {
        throw new NotImplementedException();
    }

    public void OnFullSnapshot(ExternalAccountFullSnapshot snapshot, Instant processingDt)
    {
        throw new NotImplementedException();
    }

    public void OnExecutionServiceMissedVersionEvt(Instant processingDt)
    {
        throw new NotImplementedException();
    }

    public void OnExternalAccountConnectionRestoredEvt(Instant processingDt)
    {
        throw new NotImplementedException();
    }

    public void PlaceExternalOrder(ExecutionReport er, Instant processingDt)
    {
        ExternalOrders.Add(er);
    }

    public void CancelExternalOrder(ExecutionReport er, Instant processingDt)
    {
        ExternalOcrs.Add(er);
    }

    public void ReplaceExternalOrder(OrderReplaceRequest req, ExecutionReport er, Instant processingDt)
    {
        throw new NotImplementedException();
    }

    public void OnExternalBalanceOperation(ExternalBalanceOperation balanceOperation, Instant processingDt)
    {
        throw new NotImplementedException();
    }
    
    public AccountRecordV6 AccountRecord { get; }
    public IAccountStateReadonly AccountStateReadonly { get; }
    public AccountType AccountType { get; }

    decimal? IAccount.GetBaseTradeSize(int contractId)
    {
        throw new NotImplementedException();
    }

    public decimal GetInvestment()
    {
        throw new NotImplementedException();
    }

    public void CreateAccount(Instant dt)
    {
        
    }

    public void ProcessBalanceOperation(NewBalanceOperation request, Instant processingDt, Guid? requestId = null)
    {
        throw new NotImplementedException();
    }

    public void ProcessExecutionReport(ExecutionReport? er, Instant processingDt)
    {
        throw new NotImplementedException();
    }

    public void PlaceOrder(NewOrderSingle order, Instant processingDt)
    {
        Nos.Add(order);
    }

    public void CancelOrder(OrderCancelRequest request, Instant processingDt)
    {
        throw new NotImplementedException();
    }

    public void ReplaceOrder(OrderReplaceRequest request, Instant processingDt)
    {
        
    }

    public void ProcessTrade(Trade trade, Instant processingDt)
    {
        throw new NotImplementedException();
    }

    public void OnHeartbeat(Instant processingDt)
    {
        throw new NotImplementedException();
    }

    public (decimal dailyReturn, decimal currentDrawdown) GetLiquidationInfo(IReadOnlyDictionary<int, double> lastPrices, Instant referenceDt)
    {
        throw new NotImplementedException();
    }

    public void MarkToMarketEod(IReadOnlyDictionary<int, decimal> eodPrices, Instant dt, Instant processingDt)
    {
        throw new NotImplementedException();
    }
}