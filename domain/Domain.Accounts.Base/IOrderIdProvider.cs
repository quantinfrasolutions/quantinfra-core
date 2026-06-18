namespace QuantInfra.Domain.Accounts.Base;

public interface IOrderIdProvider
{
    long GetNextOrderId();
}