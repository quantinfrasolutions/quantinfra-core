using System.ComponentModel.DataAnnotations;

namespace QuantInfra.Common.Interfaces.Api.Accounts;

public class AssignSsaToBrokerAccountRequest
{
    [Required(ErrorMessage = "Broker account is required")] public int BrokerAccountId { get; set; }
}