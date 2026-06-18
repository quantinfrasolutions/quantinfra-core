using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Databases.MarketDataHistory.Migrations
{
    /// <inheritdoc />
    public partial class AggregatedBarsRetrieval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Migrations/Sql");
            var path = Path.Combine(baseDirectory, "20251028121658_AggregatedBarsRetrieval.sql");
            var sql = File.ReadAllText(path);
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Migrations/Sql");
            var path = Path.Combine(baseDirectory, "20251028121658_AggregatedBarsRetrieval_Down.sql");
            var sql = File.ReadAllText(path);
            migrationBuilder.Sql(sql);
        }
    }
}
