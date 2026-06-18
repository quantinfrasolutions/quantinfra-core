using NodaTime;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Common.MarketData;

public class HistoryRequest
{
    public IdType IdType { get; }
    public int Id { get; }
    public int NumberOfBaus { get; }
    public double ReserveFactor { get; }
    public Period MinResolution { get; }
    public string Timezone { get; }

    public HistoryRequest(IdType idType, int id, int numberOfBaus, double reserveFactor, Period minResolution, string timezone)
    {
        IdType = idType;
        Id = id;
        NumberOfBaus = numberOfBaus;
        ReserveFactor = reserveFactor;
        MinResolution = minResolution;
        Timezone = timezone;
    }
}

public class PeriodHistoryRequest
{
    public IdType IdType { get; }
    public long Id { get; }
    public Period Period { get; }
    
    public PeriodHistoryRequest(IdType idType, long id, Period period)
    {
        IdType = idType;
        Id = id;
        Period = period;
    }
}