using Common.Metrics;
using Disruptor.Dsl;
using NodaTime;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Domain.Events.Accounts.External;
using QuantInfra.Sdk.Accounts.ExternalAccounts;
using QuantInfra.Sdk.Trading.ExternalAccounts;
using QuantInfra.Sdk.Trading.Infrastructure;

namespace QuantInfra.Services.ExecutionCore;

public class DisruptorPublisher(Disruptor<IncomingDisruptorMessage> disruptor, IClock clock) : ITradingClientResponsesHandler
{
    public void OnConnect(int accountId) =>
        disruptor.PublishParsedMessage(new ExternalAccountConnectionRestoredEvt(accountId, clock.GetCurrentInstant()), MetricsUtils.GetUnixMicro());

    public void OnExecutionReport(ExternalExecutionReport er, long receivedAt, long swReceivedAt) =>
        disruptor.PublishParsedMessage(new ExternalExecutionReportEvt(er.AccountId, er, clock.GetCurrentInstant()), swReceivedAt);

    public void OnOrderCancelReject(ExternalOrderCancelReject ocr, long receivedAt, long swReceivedAt) =>
        disruptor.PublishParsedMessage(new ExternalOrderCancelRejectEvt(ocr.AccountId, ocr, clock.GetCurrentInstant()), swReceivedAt);

    public void OnTrade(ExternalTradeRecord trade, long receivedAt, long swReceivedAt) =>
        disruptor.PublishParsedMessage(new ExternalTradeEvt(trade.AccountId, trade, clock.GetCurrentInstant()), swReceivedAt);

    public void OnBalanceOperation(ExternalBalanceOperation bo, long receivedAt, long swReceivedAt) =>
        disruptor.PublishParsedMessage(new ExternalBalanceOperationEvt(bo.AccountId, bo, clock.GetCurrentInstant()), swReceivedAt);

    public void OnOrdersSnapshotReceived(int accountId, bool success, ExternalAccountOrdersSnapshot? snapshot, long receivedAt, long swReceivedAt) =>
        disruptor.PublishParsedMessage(new ExternalAccountOrdersSnapshotEvt(accountId, snapshot, success, clock.GetCurrentInstant()), swReceivedAt);

    public void OnFullSnapshotReceived(int accountId, bool success, ExternalAccountFullSnapshot? snapshot, long receivedAt, long swReceivedAt) =>
        disruptor.PublishParsedMessage(new ExternalAccountFullSnapshotEvt(accountId, snapshot, success, clock.GetCurrentInstant()), swReceivedAt);
}