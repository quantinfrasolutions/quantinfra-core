using Microsoft.EntityFrameworkCore;
using QuantInfra.Databases.Main.Models.History;

namespace QuantInfra.Databases.Main.Models.Events;

public static class Relations
{
    public static ModelBuilder AddEventsRelations(this ModelBuilder modelBuilder)
    {
        Event.CreateRelations(modelBuilder);
        ExecutionReportConfiguration.CreateRelations(modelBuilder);
        ShareCountUpdate.CreateRelations(modelBuilder);
        SharePriceUpdate.CreateRelations(modelBuilder);
        TradeConfiguration.CreateRelations(modelBuilder);
        ExternalTradeConfiguration.CreateRelations(modelBuilder);
        return modelBuilder;
    }
}