using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace QuantInfra.Databases.MarketDataHistory.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Migrations/Sql");
            var path = Path.Combine(baseDirectory, "20260717153944_Initial.sql");
            var sql = File.ReadAllText(path);
            migrationBuilder.Sql(sql);
        }
    }
}
