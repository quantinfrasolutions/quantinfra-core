using Microsoft.EntityFrameworkCore;

namespace QuantInfra.Databases.Main.Models.Infrastructure
{
	public static class Relations
	{
		public static ModelBuilder AddInfrastructureRelations(this ModelBuilder modelBuilder)
		{
			return modelBuilder;
        }
	}
}

