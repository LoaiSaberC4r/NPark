using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPark.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TicketArchieve : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketArchive",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VehicleNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExceedPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    CollectedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsCollected = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CollectedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UniqueGuidPart = table.Column<byte[]>(type: "BINARY(4)", nullable: false),
                    UniqueCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExitGateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsCashierCollected = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsSubscriber = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SubscriberNationalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CardNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketArchive", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketArchive_ParkingGate_GateId",
                        column: x => x.GateId,
                        principalTable: "ParkingGate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketArchive_Users_CollectedBy",
                        column: x => x.CollectedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketArchive_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketArchive_CollectedBy",
                table: "TicketArchive",
                column: "CollectedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TicketArchive_GateId",
                table: "TicketArchive",
                column: "GateId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketArchive_UniqueGuidPart",
                table: "TicketArchive",
                column: "UniqueGuidPart",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketArchive_UserId",
                table: "TicketArchive",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketArchive");
        }
    }
}
