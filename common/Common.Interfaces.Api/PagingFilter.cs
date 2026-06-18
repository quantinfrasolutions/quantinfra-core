namespace QuantInfra.Common.Interfaces.Api;

public class PagingFilter
{
    public int Limit { get; set; } = 50;
    public int Offset { get; set; } = 0;
}