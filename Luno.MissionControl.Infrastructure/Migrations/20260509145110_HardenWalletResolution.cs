using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Luno.MissionControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenWalletResolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseAccountId",
                table: "AccountPreferences");

            migrationBuilder.RenameColumn(
                name: "CounterAccountId",
                table: "AccountPreferences",
                newName: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "AccountPreferences",
                newName: "CounterAccountId");

            migrationBuilder.AddColumn<long>(
                name: "BaseAccountId",
                table: "AccountPreferences",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
