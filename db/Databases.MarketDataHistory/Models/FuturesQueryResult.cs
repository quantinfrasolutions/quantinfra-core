using System;
using System.ComponentModel.DataAnnotations.Schema;
using Common.MarketData;
using Microsoft.EntityFrameworkCore;

namespace Databases.MarketDataHistory.Models
{
	// [Keyless]
	// public record FuturesQueryResult : PriceQueryResult
 //    {
	// 	[Column("contract_id")]
	// 	public long ContractId { get; set; }
	// 	[Column("period")]
	// 	public long Period { get; set; }
 //
 //        public ExchangeBar ToExchangeBar() =>
	// 		base.ToExchangeBar(ContractId);
 //    }
}

