namespace QuantInfra.Domain.Accounts.Base;

public interface IBalanceOperationIdProvider
{
    int GetNextBalanceOperationId();
}