using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingAPP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingBalanceAfterTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BalanceAfterTransaction",
                table: "Transactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BalanceAfterTransaction",
                table: "Transactions");
        }
    }
}
