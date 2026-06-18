using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Databases.MarketDataHistory.Models.CorporateEvents
{
	[Table("dividends")]
	public class Dividend
	{
		[Key]
		[Required]
		[Column("dividend_id")]
		public long Id { get; set; }
		[Required]
		[Column("contract_id")]
		public long ContractId { get; set; }
		[Required]
		[Column("ex_dividend_date")]
		public Instant ExDividendDate { get; set; }
		[Required]
		[Column("amount")]
		public double Amount { get; set; }


        public static void CreateRelations(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Dividend>()
                .HasIndex(d => d.Id);

            modelBuilder.Entity<Dividend>()
                .HasIndex(d => d.ContractId);

            modelBuilder.Entity<Dividend>()
                .HasIndex(d => d.ExDividendDate);

        }
    }
}

