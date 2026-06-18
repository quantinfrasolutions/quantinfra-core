using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Databases.MarketDataHistory.Models.CorporateEvents
{
	[Table("splits")]
	public class Split
	{
		[Key]
		[Required]
		[Column("split_id")]
		public long Id { get; set; }
		[Required]
		[Column("contract_id")]
		public long ContractId { get; set; }
		[Required]
		[Column("dt")]
		public Instant Dt { get; set; }
		[Required]
		[Column("factor")]
		public double Factor { get; set; }


        public static void CreateRelations(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Split>()
                .HasIndex(c => c.ContractId);

            modelBuilder.Entity<Split>()
                .HasIndex(c => c.Dt);
        }
    }
}

