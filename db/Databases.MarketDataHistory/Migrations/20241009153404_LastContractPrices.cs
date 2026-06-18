using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Databases.MarketDataHistory.Migrations
{
    /// <inheritdoc />
    public partial class LastContractPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Migrations/Sql");
            var path = Path.Combine(baseDirectory, "20241009153404_LastContractPrices.sql");
            var sql = File.ReadAllText(path);
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Migrations/Sql");
            var path = Path.Combine(baseDirectory, "20241009153404_LastContractPrices_Down.sql");
            var sql = File.ReadAllText(path);
            migrationBuilder.Sql(sql);
        }
    }
}
