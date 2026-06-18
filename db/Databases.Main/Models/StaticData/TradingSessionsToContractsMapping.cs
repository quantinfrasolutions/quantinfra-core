// using System;
// using System.ComponentModel.DataAnnotations;
// using System.ComponentModel.DataAnnotations.Schema;
// using Microsoft.EntityFrameworkCore;
// using QuantInfra.Databases.Main.Models.StaticData;
//
// namespace Databases.Main.Models.StaticData
// {
// 	[Table("trading_sessions_contracts")]
//     [PrimaryKey(nameof(TemplateId), nameof(TradingSessionId))]
//     public class TradingSessionsToContractsMapping
// 	{		
// 		[Column("template_id")]
// 		[Required]
// 		public long TemplateId { get; set; }		
// 		[Column("trading_session_id")]
// 		[Required]
// 		public long TradingSessionId { get; set; }
//
// 		public virtual Contracts.ContractTemplate ContractTemplate { get; set; } = default!;
// 		public virtual TradingSession TradingSession { get; set; } = default!;
// 	}
// }
//
