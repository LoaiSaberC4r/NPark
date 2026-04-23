using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPark.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCamera : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ONVIFCameraSettingId",
                table: "ParkingGate",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ONVIFCameraSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UrlRequest = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ONVIFCameraSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingGate_ONVIFCameraSettingId",
                table: "ParkingGate",
                column: "ONVIFCameraSettingId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingGate_ONVIFCameraSettings_ONVIFCameraSettingId",
                table: "ParkingGate",
                column: "ONVIFCameraSettingId",
                principalTable: "ONVIFCameraSettings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingGate_ONVIFCameraSettings_ONVIFCameraSettingId",
                table: "ParkingGate");

            migrationBuilder.DropTable(
                name: "ONVIFCameraSettings");

            migrationBuilder.DropIndex(
                name: "IX_ParkingGate_ONVIFCameraSettingId",
                table: "ParkingGate");

            migrationBuilder.DropColumn(
                name: "ONVIFCameraSettingId",
                table: "ParkingGate");
        }
    }
}
