using System.Collections.Generic;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Commands.Accounts.AccountsService;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Domain.Queries.MarketData;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Domain.Accounts.Base.CommandHandlers;

public class EndOfDay : ICommandHandler<RunEndOfDayCmd>
{
    private readonly IQueryBus _queryBus;
    private IClock _clock;

    public EndOfDay(IQueryBus queryBus, IClock clock)
    {
        _queryBus = queryBus;
        _clock = clock;
    }

    public void Handle(RunEndOfDayCmd cmd)
    {
        var prices = _queryBus.Query<GetLastKnownContractPrices, IReadOnlyDictionary<int, decimal>>(new());
        var accounts = _queryBus.Query<GetAccountIdsForEndOfDay, IReadOnlyCollection<int>>(new());
        foreach (var accId in accounts)
        {
            var account = _queryBus.Query<GetAccount, IAccount?>(new (accId));
            account?.MarkToMarketEod(prices, cmd.ReferenceDt, _clock.GetCurrentInstant());
        }
    }
}