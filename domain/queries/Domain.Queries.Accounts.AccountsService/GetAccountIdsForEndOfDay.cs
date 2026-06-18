using System.Collections.Generic;
using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Domain.Queries.Accounts.AccountsService;

public record GetAccountIdsForEndOfDay() : IQuery<IReadOnlyCollection<int>>;