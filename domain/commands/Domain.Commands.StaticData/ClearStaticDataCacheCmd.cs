using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Domain.Commands.StaticData;

public record ClearStaticDataCacheCmd(string AccountServiceName, Guid RequestId) : ICommand
{
    public static ClearStaticDataCacheCmd Create(string accountServiceName) => new(accountServiceName, Guid.NewGuid());
}