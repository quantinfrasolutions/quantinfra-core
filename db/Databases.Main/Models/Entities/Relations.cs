using Microsoft.EntityFrameworkCore;

namespace QuantInfra.Databases.Main.Models.Entities;

public static class Relations
{
    public static ModelBuilder AddEntitiesRelations(this ModelBuilder modelBuilder)
    {
        AccountConfiguration.CreateRelations(modelBuilder);
        StrategyConfiguration.CreateRelations(modelBuilder);
        SubaccountConfiguration.CreateRelations(modelBuilder);
        return modelBuilder;
    }
}