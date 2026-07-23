namespace QuantInfra.Connectors.Ibkr.Interfaces;

public class IbkrBalance
{
    public IbkrBalance() { }

    public IbkrBalance(string currency, decimal cashBalance)
    {
        Currency = currency;
        CashBalance = cashBalance;
    }

    public string Currency { get; init; }
    public decimal CashBalance { get; init; }
}