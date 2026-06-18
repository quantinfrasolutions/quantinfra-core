using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Domain.Events.Accounts.External;

public interface IExternalAccountEvent : IEvent
{
    int AccountId { get; }
}