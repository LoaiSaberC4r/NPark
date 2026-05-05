using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPark.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addExitGateToArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TicketArchive_ExitGateId",
                table: "TicketArchive",
                column: "ExitGateId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketArchive_ParkingGate_ExitGateId",
                table: "TicketArchive",
                column: "ExitGateId",
                principalTable: "ParkingGate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketArchive_ParkingGate_ExitGateId",
                table: "TicketArchive");

            migrationBuilder.DropIndex(
                name: "IX_TicketArchive_ExitGateId",
                table: "TicketArchive");
        }
    }
}
