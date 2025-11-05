using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPark.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddParkingMembershipAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VehicleImage",
                table: "ParkingMemberships");

            migrationBuilder.AlterColumn<string>(
                name: "CardNumber",
                table: "ParkingMemberships",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "ParkingMembershipsAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParkingMembershipsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingMembershipsAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingMembershipsAttachments_ParkingMemberships_ParkingMembershipsId",
                        column: x => x.ParkingMembershipsId,
                        principalTable: "ParkingMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingMembershipsAttachments_ParkingMembershipsId",
                table: "ParkingMembershipsAttachments",
                column: "ParkingMembershipsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingMembershipsAttachments");

            migrationBuilder.AlterColumn<int>(
                name: "CardNumber",
                table: "ParkingMemberships",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "VehicleImage",
                table: "ParkingMemberships",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
