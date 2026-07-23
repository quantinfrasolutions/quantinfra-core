using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Connectors.Ibkr.Interfaces;
/**
 * @brief The security's type:
 *      STK - stock (or ETF)
 *      OPT - option
 *      FUT - future
 *      IND - index
 *      FOP - futures option
 *      CASH - forex pair
 *      BAG - combo
 *      WAR - warrant
 *      BOND- bond
 *      CMDTY- commodity
 *      NEWS- news
 *		FUND- mutual fund
 */
public enum SecType
{
    STK,
    OPT,
    FUT,
    IND,
    FOP,
    CASH,
    BAG,
    WAR,
    BOND,
    CMDTY,
    NEWS,
    FUND
}

public static class SecTypeExtensions
{
    public static SecurityType ToSecurityType(this SecType t) => t switch
    {
        SecType.STK => SecurityType.Stock,
        SecType.FUT => SecurityType.Futures,
        SecType.CASH => SecurityType.FX,
        _ => throw new NotSupportedException()
    };

    public static SecType ToSecurityType(this SecurityType t) => t switch
    {
        SecurityType.Stock => SecType.STK,
        SecurityType.Futures => SecType.FUT,
        SecurityType.FX => SecType.CASH,
        _ => throw new NotSupportedException()
    };

    public static SecType ToSecurityType(this string s) =>
        Enum.Parse<SecType>(s);

    public static string ToIBKRString(this SecType t) =>
        Enum.GetName(typeof(SecType), t);
}