using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPark.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModifySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderPriority",
                table: "PricingSchemes");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "GracePeriod",
                table: "ParkingSystemConfigurations",
                type: "time",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GracePeriod",
                table: "ParkingSystemConfigurations");

            migrationBuilder.AddColumn<int>(
                name: "OrderPriority",
                table: "PricingSchemes",
                type: "int",
                nullable: true);
        }
    }
}
