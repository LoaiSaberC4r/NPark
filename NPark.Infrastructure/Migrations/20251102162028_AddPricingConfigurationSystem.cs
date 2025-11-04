using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPark.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingConfigurationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParkingSystemConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntryGatesCount = table.Column<int>(type: "int", nullable: false),
                    ExitGatesCount = table.Column<int>(type: "int", nullable: false),
                    AllowedParkingSlots = table.Column<int>(type: "int", nullable: true),
                    PriceType = table.Column<int>(type: "int", nullable: false),
                    VehiclePassengerData = table.Column<int>(type: "int", nullable: false),
                    PrintType = table.Column<int>(type: "int", nullable: false),
                    PricingSchemaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateTimeFlag = table.Column<bool>(type: "bit", nullable: false),
                    TicketIdFlag = table.Column<bool>(type: "bit", nullable: false),
                    FeesFlag = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingSystemConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingSystemConfigurations_PricingSchemes_PricingSchemaId",
                        column: x => x.PricingSchemaId,
                        principalTable: "PricingSchemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSystemConfigurations_PricingSchemaId",
                table: "ParkingSystemConfigurations",
                column: "PricingSchemaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingSystemConfigurations");
        }
    }
}
