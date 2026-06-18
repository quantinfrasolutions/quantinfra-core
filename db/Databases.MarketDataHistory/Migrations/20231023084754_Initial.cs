using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Databases.MarketDataHistory.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Migrations/Sql");
            var path = Path.Combine(baseDirectory, "20231023084754_Initial.sql");
            var sql = File.ReadAllText(path);
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
