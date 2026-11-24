using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPark.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingSuperVisorRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketCollectionTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    GateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollectedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalItemsCollected = table.Column<int>(type: "int", nullable: false),
                    TotalAmountCollected = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketCollectionTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketCollectionTransactions_ParkingGate_GateId",
                        column: x => x.GateId,
                        principalTable: "ParkingGate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketCollectionTransactions_Users_CollectedBy",
                        column: x => x.CollectedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TicketCollectionTransactionLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketCollectionTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsibleUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemsCount = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketCollectionTransactionLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketCollectionTransactionLines_TicketCollectionTransactions_TicketCollectionTransactionId",
                        column: x => x.TicketCollectionTransactionId,
                        principalTable: "TicketCollectionTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketCollectionTransactionLines_Users_ResponsibleUserId",
                        column: x => x.ResponsibleUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TicketCollectionTransactionTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketCollectionTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsibleUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    TicketIdSnapshot = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketUniqueCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TicketStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TicketEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OriginalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExceedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountCollected = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VehicleNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CardNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsSubscriber = table.Column<bool>(type: "bit", nullable: false),
                    SubscriberNationalId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TicketGateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketExitGateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketCollectionTransactionTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketCollectionTransactionTickets_TicketCollectionTransactions_TicketCollectionTransactionId",
                        column: x => x.TicketCollectionTransactionId,
                        principalTable: "TicketCollectionTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketCollectionTransactionTickets_Users_ResponsibleUserId",
                        column: x => x.ResponsibleUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketCollectionTransactionLines_ResponsibleUserId",
                table: "TicketCollectionTransactionLines",
                column: "ResponsibleUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketCollectionTransactionLines_TicketCollectionTransactionId_ResponsibleUserId",
                table: "TicketCollectionTransactionLines",
                columns: new[] { "TicketCollectionTransactionId", "ResponsibleUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketCollectionTransactions_CollectedBy",
                table: "TicketCollectionTransactions",
                column: "CollectedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TicketCollectionTransactions_GateId",
                table: "TicketCollectionTransactions",
                column: "GateId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketCollectionTransactions_Type_GateId_CollectedAt",
                table: "TicketCollectionTransactions",
                columns: new[] { "Type", "GateId", "CollectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketCollectionTransactionTickets_ResponsibleUserId",
                table: "TicketCollectionTransactionTickets",
                column: "ResponsibleUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketCollectionTransactionTickets_TicketCollectionTransactionId_Source_TicketIdSnapshot",
                table: "TicketCollectionTransactionTickets",
                columns: new[] { "TicketCollectionTransactionId", "Source", "TicketIdSnapshot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketCollectionTransactionTickets_TicketUniqueCode",
                table: "TicketCollectionTransactionTickets",
                column: "TicketUniqueCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketCollectionTransactionLines");

            migrationBuilder.DropTable(
                name: "TicketCollectionTransactionTickets");

            migrationBuilder.DropTable(
                name: "TicketCollectionTransactions");
        }
    }
}
