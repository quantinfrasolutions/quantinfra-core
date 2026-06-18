using System;
using NodaTime;
using QuantInfra.Sdk.Accounting;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Primary;

public record BalanceOperationProcessedEvt(
	long EventId,
	int AccountId,
	long Version,
	BalanceOperation BalanceOperation,
	Instant Timestamp,
	Guid? RequestId
) : IAccountEventBase;