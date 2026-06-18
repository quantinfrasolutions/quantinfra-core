using Microsoft.EntityFrameworkCore;

namespace QuantInfra.Databases.Main.Models.StaticData
{
	public static class Relations
	{
		public static ModelBuilder AddStaticDataRelations(this ModelBuilder modelBuilder)
		{
			AssetConfiguration.CreateRelations(modelBuilder);
			BrokerConfiguration.CreateRelations(modelBuilder);
			CommissionStructureConfiguration.CreateRelations(modelBuilder);
			ContractConfiguration.CreateRelations(modelBuilder);
			ContractTemplateConfiguration.CreateRelations(modelBuilder);
			CurrencyConfiguration.CreateRelations(modelBuilder);
			DatafeedConfiguration.CreateRelations(modelBuilder);
			ExchangeConfiguration.CreateRelations(modelBuilder);
			StreamConfiguration.CreateRelations(modelBuilder);
			TradingSessionConfiguration.CreateRelations(modelBuilder);
			TradingSessionIntervalConfiguration.CreateRelations(modelBuilder);

			return modelBuilder;
        }
	}
}

