using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPark.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketExceedTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketIdSnapshot = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketUniqueCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TicketStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TicketEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TicketPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TicketOldExceedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPriceAfterExceed = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VehicleNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CardNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsSubscriber = table.Column<bool>(type: "bit", nullable: false),
                    SubscriberNationalId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TicketGateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketExitGateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExceedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserCollectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuperVisorCollectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketExceedTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketExceedTransactions_ParkingGate_GateId",
                        column: x => x.GateId,
                        principalTable: "ParkingGate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketExceedTransactions_Users_SuperVisorCollectorId",
                        column: x => x.SuperVisorCollectorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketExceedTransactions_Users_UserCollectId",
                        column: x => x.UserCollectId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TicketExceedTransactionSanctions",
                columns: table => new
                {
                    TicketExceedTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SanctionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketExceedTransactionSanctions", x => new { x.TicketExceedTransactionId, x.SanctionId });
                    table.ForeignKey(
                        name: "FK_TicketExceedTransactionSanctions_Sanctions_SanctionId",
                        column: x => x.SanctionId,
                        principalTable: "Sanctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketExceedTransactionSanctions_TicketExceedTransactions_TicketExceedTransactionId",
                        column: x => x.TicketExceedTransactionId,
                        principalTable: "TicketExceedTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketExceedTransactions_CreatedAt",
                table: "TicketExceedTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TicketExceedTransactions_GateId",
                table: "TicketExceedTransactions",
                column: "GateId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketExceedTransactions_SuperVisorCollectorId",
                table: "TicketExceedTransactions",
                column: "SuperVisorCollectorId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketExceedTransactions_TicketIdSnapshot",
                table: "TicketExceedTransactions",
                column: "TicketIdSnapshot",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketExceedTransactions_UserCollectId",
                table: "TicketExceedTransactions",
                column: "UserCollectId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketExceedTransactionSanctions_SanctionId",
                table: "TicketExceedTransactionSanctions",
                column: "SanctionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketExceedTransactionSanctions");

            migrationBuilder.DropTable(
                name: "TicketExceedTransactions");
        }
    }
}
