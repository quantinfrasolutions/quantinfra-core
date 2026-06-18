// using System;
// using System.ComponentModel.DataAnnotations.Schema;
// using Common.MarketData;
// using Microsoft.EntityFrameworkCore;
//
// namespace Databases.MarketDataHistory.Models
// {
//     [Keyless]
// 	public record UniversalPriceQueryResult : PriceQueryResult
//     {
//         [Column("total_dividends")]
//         public double TotalDividends { get; set; }
//         [Column("factor")]
//         public double SplitFactor { get; set; }
//         [Column("contract_id")]
//         public long ContractId { get; set; }
//         [Column("period")]
//         public long Period { get; set; }        
//
//         public ExchangeBar ToExchangeBar() =>
//             base.ToExchangeBar(ContractId);
//     }
// }
//
