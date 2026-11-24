using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPark.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ParkingFinancialIntegrityV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketExceedTransactions_TicketIdSnapshot",
                table: "TicketExceedTransactions");

            migrationBuilder.DropIndex(
                name: "IX_TicketCollectionTransactionTickets_TicketCollectionTransactionId_Source_TicketIdSnapshot",
                table: "TicketCollectionTransactionTickets");

            migrationBuilder.AlterColumn<Guid>(
                name: "ResponsibleUserId",
                table: "TicketCollectionTransactionTickets",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "ResponsibleUserId",
                table: "TicketCollectionTransactionLines",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateSequence(
                name: "GateCommandIds",
                minValue: 1L,
                maxValue: 9223372036854775807L);

            migrationBuilder.AddColumn<Guid>(
                name: "BasePaymentCashierId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BasePaymentConfirmedAtUtc",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BasePaymentGateId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BasePaymentStatus",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "BasePriceCalculated",
                table: "Tickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EntryConfirmationStatus",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EntryConfirmedAtUtc",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricingModeSnapshot",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Tickets",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ValidExitDate",
                table: "Tickets",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "AppliedSanctionsJson",
                table: "TicketExceedTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Classification",
                table: "TicketExceedTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "TicketExceedTransactions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentCashierId",
                table: "TicketExceedTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentConfirmedAtUtc",
                table: "TicketExceedTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentGateId",
                table: "TicketExceedTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "TicketExceedTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PenaltyFromDate",
                table: "TicketExceedTransactions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PenaltyThroughDate",
                table: "TicketExceedTransactions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TicketExceedTransactions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "FinancialItemType",
                table: "TicketCollectionTransactionTickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceRecordId",
                table: "TicketCollectionTransactionTickets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CollectedDate",
                table: "TicketArchive",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<Guid>(
                name: "BasePaymentCashierId",
                table: "TicketArchive",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BasePaymentConfirmedAtUtc",
                table: "TicketArchive",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BasePaymentGateId",
                table: "TicketArchive",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BasePaymentStatus",
                table: "TicketArchive",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntryConfirmationStatus",
                table: "TicketArchive",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EntryConfirmedAtUtc",
                table: "TicketArchive",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExitOperationId",
                table: "TicketArchive",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricingModeSnapshot",
                table: "TicketArchive",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TicketArchive",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ValidExitDate",
                table: "TicketArchive",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.Sql("""
                EXEC(N'
                UPDATE [Tickets] SET [ValidExitDate] = CONVERT(date, [StartDate]),
                    [BasePriceCalculated] = CASE WHEN [IsCashierCollected] = 1 THEN 1 ELSE 0 END;
                UPDATE [TicketArchive] SET [ValidExitDate] = CONVERT(date, [StartDate]);

                IF EXISTS (
                    SELECT 1 FROM [TicketCollectionTransactionTickets] AS item
                    WHERE item.[Source] NOT IN (1, 2, 3)
                       OR item.[TicketIdSnapshot] = ''00000000-0000-0000-0000-000000000000''
                       OR (item.[Source] = 3 AND NOT EXISTS (
                           SELECT 1 FROM [TicketExceedTransactions] AS payment
                           WHERE payment.[Id] = item.[TicketIdSnapshot]
                             AND payment.[TicketIdSnapshot] = item.[TicketIdSnapshot]))
                )
                    THROW 51001, ''Unverified historical settlement references. Run the NPark V2 migration preflight.'', 1;

                UPDATE [TicketCollectionTransactionTickets]
                SET [SourceRecordId] = [TicketIdSnapshot],
                    [FinancialItemType] = CASE WHEN [Source] = 3 THEN 2 ELSE 1 END;

                IF EXISTS (
                    SELECT 1 FROM [TicketCollectionTransactionTickets]
                    GROUP BY [FinancialItemType], [SourceRecordId] HAVING COUNT_BIG(*) > 1
                )
                    THROW 51002, ''Duplicate historical settlement identities. Run the NPark V2 migration preflight.'', 1;
                ');
                """);

            migrationBuilder.CreateTable(
                name: "GateOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommandId = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR [GateCommandIds]"),
                    TicketIdSnapshot = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReservedTicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReservedControllerId = table.Column<int>(type: "int", nullable: true),
                    GateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ControllerId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DispatchStartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    AckStatusCode = table.Column<byte>(type: "tinyint", nullable: true),
                    ReconciledBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReconciliationEvidence = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReconciledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GateOperations", x => x.Id);
                    table.CheckConstraint("CK_GateOperations_Controller", "[ControllerId] BETWEEN 1 AND 65535 AND [CommandId] > 0");
                });

            migrationBuilder.CreateTable(
                name: "OvernightPenaltyCoverages",
                columns: table => new
                {
                    TicketIdSnapshot = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OvernightPenaltyCoverages", x => new { x.TicketIdSnapshot, x.BusinessDate });
                    table.ForeignKey(
                        name: "FK_OvernightPenaltyCoverages_TicketExceedTransactions_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "TicketExceedTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransactionReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionReceipts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketExceedTransactions_TicketIdSnapshot",
                table: "TicketExceedTransactions",
                column: "TicketIdSnapshot");

            migrationBuilder.CreateIndex(
                name: "IX_TicketExceedTransactions_TicketIdSnapshot_IdempotencyKey",
                table: "TicketExceedTransactions",
                columns: new[] { "TicketIdSnapshot", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TicketCollectionTransactionTickets_FinancialItemType_SourceRecordId",
                table: "TicketCollectionTransactionTickets",
                columns: new[] { "FinancialItemType", "SourceRecordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketCollectionTransactionTickets_TicketCollectionTransactionId_Source_SourceRecordId",
                table: "TicketCollectionTransactionTickets",
                columns: new[] { "TicketCollectionTransactionId", "Source", "SourceRecordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketCollectionTransactionTickets_TicketIdSnapshot",
                table: "TicketCollectionTransactionTickets",
                column: "TicketIdSnapshot");

            migrationBuilder.CreateIndex(
                name: "IX_GateOperations_CommandId",
                table: "GateOperations",
                column: "CommandId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GateOperations_ControllerId",
                table: "GateOperations",
                column: "ControllerId");

            migrationBuilder.CreateIndex(
                name: "IX_GateOperations_ReservedControllerId",
                table: "GateOperations",
                column: "ReservedControllerId",
                unique: true,
                filter: "[ReservedControllerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GateOperations_ReservedTicketId",
                table: "GateOperations",
                column: "ReservedTicketId",
                unique: true,
                filter: "[ReservedTicketId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GateOperations_Status_CreatedAtUtc",
                table: "GateOperations",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GateOperations_TicketIdSnapshot_Type",
                table: "GateOperations",
                columns: new[] { "TicketIdSnapshot", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_OvernightPenaltyCoverages_PaymentId",
                table: "OvernightPenaltyCoverages",
                column: "PaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [TicketExceedTransactions] GROUP BY [TicketIdSnapshot] HAVING COUNT_BIG(*) > 1)
                    THROW 51003, 'Rollback cannot preserve multiple exceed payments per ticket. Restore a reviewed backup or use a forward fix.', 1;
                IF EXISTS (SELECT 1 FROM [GateOperations]) OR EXISTS (SELECT 1 FROM [TransactionReceipts])
                    THROW 51004, 'Rollback would remove parking operation and payment metadata. Restore a reviewed backup or use a forward fix.', 1;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "ResponsibleUserId",
                table: "TicketCollectionTransactionTickets",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ResponsibleUserId",
                table: "TicketCollectionTransactionLines",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.DropTable(
                name: "GateOperations");

            migrationBuilder.DropTable(
                name: "OvernightPenaltyCoverages");

            migrationBuilder.DropTable(
                name: "TransactionReceipts");

            migrationBuilder.DropIndex(
                name: "IX_TicketExceedTransactions_TicketIdSnapshot",
                table: "TicketExceedTransactions");

            migrationBuilder.DropIndex(
                name: "IX_TicketExceedTransactions_TicketIdSnapshot_IdempotencyKey",
                table: "TicketExceedTransactions");

            migrationBuilder.DropIndex(
                name: "IX_TicketCollectionTransactionTickets_FinancialItemType_SourceRecordId",
                table: "TicketCollectionTransactionTickets");

            migrationBuilder.DropIndex(
                name: "IX_TicketCollectionTransactionTickets_TicketCollectionTransactionId_Source_SourceRecordId",
                table: "TicketCollectionTransactionTickets");

            migrationBuilder.DropIndex(
                name: "IX_TicketCollectionTransactionTickets_TicketIdSnapshot",
                table: "TicketCollectionTransactionTickets");

            migrationBuilder.DropColumn(
                name: "BasePaymentCashierId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "BasePaymentConfirmedAtUtc",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "BasePaymentGateId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "BasePaymentStatus",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "BasePriceCalculated",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EntryConfirmationStatus",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EntryConfirmedAtUtc",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "PricingModeSnapshot",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ValidExitDate",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "AppliedSanctionsJson",
                table: "TicketExceedTransactions");

            migrationBuilder.DropColumn(
                name: "Classification",
                table: "TicketExceedTransactions");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "TicketExceedTransactions");

            migrationBuilder.DropColumn(
                name: "PaymentCashierId",
                table: "TicketExceedTransactions");

            migrationBuilder.DropColumn(
                name: "PaymentConfirmedAtUtc",
                table: "TicketExceedTransactions");

            migrationBuilder.DropColumn(
                name: "PaymentGateId",
                table: "TicketExceedTransactions");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "TicketExceedTransactions");

            migrationBuilder.DropColumn(
                name: "PenaltyFromDate",
                table: "TicketExceedTransactions");

            migrationBuilder.DropColumn(
                name: "PenaltyThroughDate",
                table: "TicketExceedTransactions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TicketExceedTransactions");

            migrationBuilder.DropColumn(
                name: "FinancialItemType",
                table: "TicketCollectionTransactionTickets");

            migrationBuilder.DropColumn(
                name: "SourceRecordId",
                table: "TicketCollectionTransactionTickets");

            migrationBuilder.DropColumn(
                name: "BasePaymentCashierId",
                table: "TicketArchive");

            migrationBuilder.DropColumn(
                name: "BasePaymentConfirmedAtUtc",
                table: "TicketArchive");

            migrationBuilder.DropColumn(
                name: "BasePaymentGateId",
                table: "TicketArchive");

            migrationBuilder.DropColumn(
                name: "BasePaymentStatus",
                table: "TicketArchive");

            migrationBuilder.DropColumn(
                name: "EntryConfirmationStatus",
                table: "TicketArchive");

            migrationBuilder.DropColumn(
                name: "EntryConfirmedAtUtc",
                table: "TicketArchive");

            migrationBuilder.DropColumn(
                name: "ExitOperationId",
                table: "TicketArchive");

            migrationBuilder.DropColumn(
                name: "PricingModeSnapshot",
                table: "TicketArchive");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TicketArchive");

            migrationBuilder.DropColumn(
                name: "ValidExitDate",
                table: "TicketArchive");

            migrationBuilder.DropSequence(
                name: "GateCommandIds");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CollectedDate",
                table: "TicketArchive",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketExceedTransactions_TicketIdSnapshot",
                table: "TicketExceedTransactions",
                column: "TicketIdSnapshot",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketCollectionTransactionTickets_TicketCollectionTransactionId_Source_TicketIdSnapshot",
                table: "TicketCollectionTransactionTickets",
                columns: new[] { "TicketCollectionTransactionId", "Source", "TicketIdSnapshot" },
                unique: true);
        }
    }
}
