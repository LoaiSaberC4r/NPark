using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPark.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLprAndParkingEventIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAutomaticVisitorEntry",
                table: "GateOperations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "LprCaptures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ControllerId = table.Column<int>(type: "int", nullable: true),
                    LprIp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VehicleNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalizedVehicleNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LprCaptures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LprCapturesArchive",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ControllerId = table.Column<int>(type: "int", nullable: true),
                    LprIp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VehicleNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalizedVehicleNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LprCapturesArchive", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParkingHardwareEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ControllerId = table.Column<int>(type: "int", nullable: false),
                    GateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventId = table.Column<byte[]>(type: "binary(8)", nullable: false),
                    Schema = table.Column<byte>(type: "tinyint", nullable: false),
                    Lane = table.Column<byte>(type: "tinyint", nullable: false),
                    Outcome = table.Column<byte>(type: "tinyint", nullable: false),
                    Flags = table.Column<byte>(type: "tinyint", nullable: false),
                    CardNumber = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: true),
                    RawHardwareTimestamp = table.Column<long>(type: "bigint", nullable: false),
                    HardwareOccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RawPayload = table.Column<byte[]>(type: "varbinary(31)", maxLength: 31, nullable: false),
                    StorageAckSentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LprCaptureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GateOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProcessingStatus = table.Column<int>(type: "int", nullable: false),
                    FailureReasonCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ProcessingAttempts = table.Column<int>(type: "int", nullable: false),
                    RetryAfterUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingHardwareEvents", x => x.Id);
                    table.CheckConstraint("CK_ParkingHardwareEvents_Controller", "[ControllerId] BETWEEN 1 AND 65535");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LprCaptures_CapturedAtUtc_Id",
                table: "LprCaptures",
                columns: new[] { "CapturedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_LprCaptures_ControllerId_GateId_CapturedAtUtc_Id",
                table: "LprCaptures",
                columns: new[] { "ControllerId", "GateId", "CapturedAtUtc", "Id" },
                descending: new[] { false, false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_LprCaptures_GateId_CapturedAtUtc",
                table: "LprCaptures",
                columns: new[] { "GateId", "CapturedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LprCaptures_GateId_NormalizedVehicleNumber_CapturedAtUtc",
                table: "LprCaptures",
                columns: new[] { "GateId", "NormalizedVehicleNumber", "CapturedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LprCaptures_TicketId",
                table: "LprCaptures",
                column: "TicketId",
                filter: "[TicketId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingHardwareEvents_ControllerId_EventId",
                table: "ParkingHardwareEvents",
                columns: new[] { "ControllerId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingHardwareEvents_LprCaptureId",
                table: "ParkingHardwareEvents",
                column: "LprCaptureId",
                unique: true,
                filter: "[LprCaptureId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingHardwareEvents_ProcessingStatus_ReceivedAtUtc_Id",
                table: "ParkingHardwareEvents",
                columns: new[] { "ProcessingStatus", "ReceivedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingHardwareEvents_ReceivedAtUtc_Id",
                table: "ParkingHardwareEvents",
                columns: new[] { "ReceivedAtUtc", "Id" },
                filter: "[StorageAckSentAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingHardwareEvents_TicketId",
                table: "ParkingHardwareEvents",
                column: "TicketId",
                unique: true,
                filter: "[TicketId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LprCaptures");

            migrationBuilder.DropTable(
                name: "LprCapturesArchive");

            migrationBuilder.DropTable(
                name: "ParkingHardwareEvents");

            migrationBuilder.DropColumn(
                name: "IsAutomaticVisitorEntry",
                table: "GateOperations");
        }
    }
}
