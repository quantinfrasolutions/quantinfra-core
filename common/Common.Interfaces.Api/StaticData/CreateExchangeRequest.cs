using System.ComponentModel.DataAnnotations;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class CreateExchangeRequest
{
    [Required(ErrorMessage = "Exchange name is required")] public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "Timezone is required")] public string TimezoneName { get; set; } = "UTC";

    public Exchange ToExchange() => new()
    {
        Name = Name,
        TimezoneName = TimezoneName,
    };
}
