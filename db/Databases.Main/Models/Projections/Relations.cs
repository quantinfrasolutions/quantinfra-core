using Microsoft.EntityFrameworkCore;

namespace QuantInfra.Databases.Main.Models.Projections;

public static class Relations
{
    public static ModelBuilder AddProjectionsRelations(this ModelBuilder modelBuilder)
    {
        PositionHistoryConfiguration.CreateRelations(modelBuilder);
        return modelBuilder;
    }
}