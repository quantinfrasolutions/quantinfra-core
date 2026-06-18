// using Common.EventSourcing;
// using Common.Trading.Orders;
// using Domain.Events.Accounts;
//
// namespace QuantInfra.Tests.Mocks;
//
// public class MockSaveExecutionReports : 
//     IEventHandler<ExecutionReportEvt>,
//     IEventHandler<ExternalAccountExecutionReportEvt>
// {
//     public List<ExecutionReport> ExecutionReports { get; } = new();
//     public List<ExecutionReport> ExternalExecutionReports { get; } = new();
//
//     public void Handle(ExecutionReportEvt e) =>
//         ExecutionReports.Add(e.ExecutionReport);
//
//     public void Handle(ExternalAccountExecutionReportEvt e) =>
//         ExternalExecutionReports.Add(e.ExecutionReport);
// }