using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPark.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGateInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParkingGate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GateNumber = table.Column<int>(type: "int", nullable: false),
                    GateType = table.Column<int>(type: "int", nullable: false),
                    IsOccupied = table.Column<bool>(type: "bit", nullable: true),
                    OccupiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccupiedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LprIp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParkingSystemConfigurationId = table.Column<int>(type: "int", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingGate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingGate_ParkingSystemConfigurations_ParkingSystemConfigurationId",
                        column: x => x.ParkingSystemConfigurationId,
                        principalTable: "ParkingSystemConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingGate_ParkingSystemConfigurationId",
                table: "ParkingGate",
                column: "ParkingSystemConfigurationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingGate");
        }
    }
}
