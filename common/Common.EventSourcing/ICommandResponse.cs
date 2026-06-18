using System;

namespace QuantInfra.Common.EventSourcing;

public interface ICommandResponse
{
    Guid RequestId { get; }
}