using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.Trading.Infrastructure;

namespace QuantInfra.Services.ExecutionCore.Queries;

public record GetTradingClient(int AccountId): IQuery<IHostedTradingClient?>;