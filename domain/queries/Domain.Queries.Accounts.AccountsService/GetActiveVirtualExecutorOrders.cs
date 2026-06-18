using System.Collections.Generic;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.Queries.Accounts.AccountsService;

public record struct GetActiveVirtualExecutorOrders(bool VirtualAccounts) : IQuery<IReadOnlyCollection<OrderStatus>>;