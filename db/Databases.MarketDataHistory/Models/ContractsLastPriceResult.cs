using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuantInfra.Databases.MarketDataHistory.Models;

[Keyless]
public class ContractsLastPriceResult
{
    [Column("stream_id")] public long StreamId { get; set; }
    [Column("contract_id")] public long ContractId { get; set; }
    [Column("close")] public double Close { get; set; }
}