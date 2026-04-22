using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPark.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GateAssigned : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedTo",
                table: "ParkingGate",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAssigned",
                table: "ParkingGate",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingGate_AssignedTo",
                table: "ParkingGate",
                column: "AssignedTo",
                unique: true,
                filter: "[AssignedTo] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingGate_Users_AssignedTo",
                table: "ParkingGate",
                column: "AssignedTo",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingGate_Users_AssignedTo",
                table: "ParkingGate");

            migrationBuilder.DropIndex(
                name: "IX_ParkingGate_AssignedTo",
                table: "ParkingGate");

            migrationBuilder.DropColumn(
                name: "AssignedTo",
                table: "ParkingGate");

            migrationBuilder.DropColumn(
                name: "IsAssigned",
                table: "ParkingGate");
        }
    }
}
