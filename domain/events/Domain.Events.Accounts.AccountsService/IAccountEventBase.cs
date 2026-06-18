using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Domain.Events.Accounts.AccountsService;

public interface IAccountEventBase : IAggregateEvent
{        
    int AccountId { get; }
}

public interface IAccountProjectionUpdatedEvt : IProjectionUpdatedEvent
{
    int AccountId { get; }
}