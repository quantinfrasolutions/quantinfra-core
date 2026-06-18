using System;
using Microsoft.EntityFrameworkCore;

namespace Databases.MarketDataHistory.Models.CorporateEvents
{
	public static class Relations
	{
		public static ModelBuilder AddCorporateEventsRelations(this ModelBuilder modelBuilder)
		{
			Dividend.CreateRelations(modelBuilder);
			RollingContract.CreateRelations(modelBuilder);
			Split.CreateRelations(modelBuilder);

			return modelBuilder;
		}
	}
}

