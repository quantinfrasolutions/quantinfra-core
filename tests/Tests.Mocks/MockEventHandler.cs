// using Common.EventSourcing;
// using Domain.Events.Accounts;
//
// namespace QuantInfra.Tests.Mocks;
//
// public class MockEventHandler : 
//     IEventHandler<ExecutionRequestCreatedEvt>,
//     IEventHandler<ExecutionRequestUpdatedEvt>
// {
//     public List<ExecutionRequestCreatedEvt> ExecutionRequestCreatedEvts = new();
//     public List<ExecutionRequestUpdatedEvt> ExecutionRequestUpdatedEvts = new();
//
//     public void Handle(ExecutionRequestCreatedEvt e) => ExecutionRequestCreatedEvts.Add(e);
//     public void Handle(ExecutionRequestUpdatedEvt e) => ExecutionRequestUpdatedEvts.Add(e);
// }