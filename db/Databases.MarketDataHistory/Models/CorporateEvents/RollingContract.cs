using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Databases.MarketDataHistory.Models.CorporateEvents
{
	[Table("rolling_contracts")]
	public class RollingContract
	{
		[Column("id")]
		[Required]
		[Key]
		public int Id { get; set; }
		[Column("parent_contract_id")]
		[Required]
		public long ParentContractId { get; set; }
		[Column("contract_id")]
		[Required]
		public long ContractId { get; set; }
		[Column("start_dt")]
		[Required]
		public Instant StartDt { get; set; }
		[Column("end_dt")]
		[Required]
		public Instant EndDt { get; set; }


		public static void CreateRelations(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<RollingContract>()
				.HasIndex(c => c.ContractId);

            modelBuilder.Entity<RollingContract>()
                .HasIndex(c => c.ParentContractId);

            modelBuilder.Entity<RollingContract>()
                .HasIndex(c => c.StartDt);

            modelBuilder.Entity<RollingContract>()
                .HasIndex(c => c.EndDt);
        }
	}
}

