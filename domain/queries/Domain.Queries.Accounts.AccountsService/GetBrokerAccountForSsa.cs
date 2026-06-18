using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Domain.Queries.Accounts.AccountsService;

public record GetBrokerAccountForSsa(int StrategySubaccountId, int BrokerId) : IQuery<int?>;