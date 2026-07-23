using QuantInfra.Ibkr.Interfaces;

namespace QuantInfra.Connectors.Ibkr.Interfaces
{
    // ReSharper disable once InconsistentNaming
    public class IBKRContractFilter
    {
        public IBKRContractFilter() { }

        public IBKRContractFilter(IBKRContractFilter filter)
        {
            ConId = filter.ConId;
            Ticker = filter.Ticker;
            SecurityType = filter.SecurityType;
            Currency = filter.Currency;
            Exchange = filter.Exchange;
            PrimaryExchange = filter.PrimaryExchange;
            FuturesLastDateOrContractMonth = filter.FuturesLastDateOrContractMonth;
            LocalSymbol = filter.LocalSymbol;
            IncludeExpired = filter.IncludeExpired;
            SuppressValidations = filter.SuppressValidations;
            OutsideOfTradingHours = filter.OutsideOfTradingHours;
        }

        public int? ConId { get; set; }
        public string? Ticker { get; set; }
        public SecType SecurityType { get; set; }
        public string? Currency { get; set; }
        public string? Exchange { get; set; }
        public string? PrimaryExchange { get; set; }
        public string? FuturesLastDateOrContractMonth { get; set; }
        public string? LocalSymbol { get; set; }
        public bool? IncludeExpired { get; set; }
        public bool SuppressValidations { get; set; }
        public bool OutsideOfTradingHours { get; set; }

        protected bool Equals(IBKRContractFilter other)
        {
            return ConId == other.ConId && Ticker == other.Ticker && SecurityType == other.SecurityType && Currency == other.Currency && Exchange == other.Exchange && PrimaryExchange == other.PrimaryExchange && FuturesLastDateOrContractMonth == other.FuturesLastDateOrContractMonth && LocalSymbol == other.LocalSymbol && IncludeExpired == other.IncludeExpired && SuppressValidations == other.SuppressValidations && OutsideOfTradingHours == other.OutsideOfTradingHours;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((IBKRContractFilter)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(ConId);
            hashCode.Add(Ticker);
            hashCode.Add(SecurityType);
            hashCode.Add(Currency);
            hashCode.Add(Exchange);
            hashCode.Add(PrimaryExchange);
            hashCode.Add(FuturesLastDateOrContractMonth);
            hashCode.Add(LocalSymbol);
            hashCode.Add(IncludeExpired);
            hashCode.Add(SuppressValidations);
            hashCode.Add(OutsideOfTradingHours);
            return hashCode.ToHashCode();
        }

        public override string ToString() => $"ConId={ConId}, Ticker={Ticker}, SecurityType={SecurityType}, " +
                                             $"Currency={Currency}, Exchange={Exchange}, PrimaryExchange={PrimaryExchange}, " +
                                             $"FuturesLastDateOrContractMonth={FuturesLastDateOrContractMonth}, LocalSymbol={LocalSymbol}, " +
                                             $"IncludeExpired={IncludeExpired}";
    }
}

