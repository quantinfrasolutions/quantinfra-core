using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Common.MarketData;

public sealed class Open : AbstractIndicator
{
    public Open()
    {
        Id = "Open";
    }


    protected override double? Calculate(IBarStorage bars, double? price = null) =>
        price; //GetValue(bars.CurrentBar) ?? price;
}

public sealed class High : AbstractIndicator
{
    public High()
    {
        Id = "High";
    }


    protected override double? Calculate(IBarStorage bars, double? price = null) => price;
    // {
    //     var curVal = GetValue(bars.CurrentBar);
    //     return curVal.HasValue ? Math.Max(curVal.Value, price.Value) : price.Value;
    // }
}

public sealed class Low : AbstractIndicator
{
    public Low()
    {
        Id = "Low";
    }

    protected override double? Calculate(IBarStorage bars, double? price = null) => price;
    // {            
    //     var curVal = GetValue(bars.CurrentBar);
    //     return curVal.HasValue ? Math.Min(curVal.Value, price.Value) : price.Value;
    // }
}

public sealed class Close : AbstractIndicator
{
    public Close()
    {
        Id = "Close";
    }

    protected override double? Calculate(IBarStorage bars, double? price = null) => price;//GetValue(bars.CurrentBar) ?? price;
}

public sealed class Volume : AbstractIndicator
{
    public Volume()
    {
        Id = "Volume";
    }

    protected override double? Calculate(IBarStorage bars, double? price = null) => price;// (GetValue(bars.CurrentBar) ?? 0.0) + price.Value;
}