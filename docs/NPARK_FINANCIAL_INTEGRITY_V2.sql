SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    DROP INDEX [IX_TicketExceedTransactions_TicketIdSnapshot] ON [TicketExceedTransactions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    DROP INDEX [IX_TicketCollectionTransactionTickets_TicketCollectionTransactionId_Source_TicketIdSnapshot] ON [TicketCollectionTransactionTickets];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    DECLARE @var sysname;
    SELECT @var = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TicketCollectionTransactionTickets]') AND [c].[name] = N'ResponsibleUserId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [TicketCollectionTransactionTickets] DROP CONSTRAINT [' + @var + '];');
    ALTER TABLE [TicketCollectionTransactionTickets] ALTER COLUMN [ResponsibleUserId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TicketCollectionTransactionLines]') AND [c].[name] = N'ResponsibleUserId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [TicketCollectionTransactionLines] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [TicketCollectionTransactionLines] ALTER COLUMN [ResponsibleUserId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    CREATE SEQUENCE [GateCommandIds] START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 NO CYCLE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [Tickets] ADD [BasePaymentCashierId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [Tickets] ADD [BasePaymentConfirmedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [Tickets] ADD [BasePaymentGateId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [Tickets] ADD [BasePaymentStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [Tickets] ADD [BasePriceCalculated] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [Tickets] ADD [EntryConfirmationStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [Tickets] ADD [EntryConfirmedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [Tickets] ADD [PricingModeSnapshot] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [Tickets] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [Tickets] ADD [ValidExitDate] date NOT NULL DEFAULT '0001-01-01';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketExceedTransactions] ADD [AppliedSanctionsJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketExceedTransactions] ADD [Classification] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketExceedTransactions] ADD [IdempotencyKey] nvarchar(128) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketExceedTransactions] ADD [PaymentCashierId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketExceedTransactions] ADD [PaymentConfirmedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketExceedTransactions] ADD [PaymentGateId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketExceedTransactions] ADD [PaymentStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketExceedTransactions] ADD [PenaltyFromDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketExceedTransactions] ADD [PenaltyThroughDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketExceedTransactions] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketCollectionTransactionTickets] ADD [FinancialItemType] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketCollectionTransactionTickets] ADD [SourceRecordId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TicketArchive]') AND [c].[name] = N'CollectedDate');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [TicketArchive] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [TicketArchive] ALTER COLUMN [CollectedDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketArchive] ADD [BasePaymentCashierId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketArchive] ADD [BasePaymentConfirmedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketArchive] ADD [BasePaymentGateId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketArchive] ADD [BasePaymentStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketArchive] ADD [EntryConfirmationStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketArchive] ADD [EntryConfirmedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketArchive] ADD [ExitOperationId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketArchive] ADD [PricingModeSnapshot] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketArchive] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    ALTER TABLE [TicketArchive] ADD [ValidExitDate] date NOT NULL DEFAULT '0001-01-01';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    CREATE TABLE [GateOperations] (
        [Id] uniqueidentifier NOT NULL,
        [CommandId] bigint NOT NULL DEFAULT (NEXT VALUE FOR [GateCommandIds]),
        [TicketIdSnapshot] uniqueidentifier NULL,
        [ReservedTicketId] uniqueidentifier NULL,
        [ReservedControllerId] int NULL,
        [GateId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NULL,
        [ControllerId] int NOT NULL,
        [Type] int NOT NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [DispatchStartedAtUtc] datetime2 NULL,
        [PublishedAtUtc] datetime2 NULL,
        [AcknowledgedAtUtc] datetime2 NULL,
        [CompletedAtUtc] datetime2 NULL,
        [RetryCount] int NOT NULL,
        [AckStatusCode] tinyint NULL,
        [ReconciledBy] uniqueidentifier NULL,
        [ReconciliationEvidence] nvarchar(500) NULL,
        [ReconciledAtUtc] datetime2 NULL,
        [ErrorCode] nvarchar(64) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedOnUtc] datetime2 NOT NULL,
        [ModifiedOnUtc] datetime2 NULL,
        CONSTRAINT [PK_GateOperations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_GateOperations_Controller] CHECK ([ControllerId] BETWEEN 1 AND 65535 AND [CommandId] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    CREATE TABLE [OvernightPenaltyCoverages] (
        [TicketIdSnapshot] uniqueidentifier NOT NULL,
        [BusinessDate] date NOT NULL,
        [PaymentId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_OvernightPenaltyCoverages] PRIMARY KEY ([TicketIdSnapshot], [BusinessDate]),
        CONSTRAINT [FK_OvernightPenaltyCoverages_TicketExceedTransactions_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [TicketExceedTransactions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    CREATE TABLE [TransactionReceipts] (
        [Id] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_TransactionReceipts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    CREATE INDEX [IX_TicketExceedTransactions_TicketIdSnapshot] ON [TicketExceedTransactions] ([TicketIdSnapshot]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TicketExceedTransactions_TicketIdSnapshot_IdempotencyKey] ON [TicketExceedTransactions] ([TicketIdSnapshot], [IdempotencyKey]) WHERE [IdempotencyKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TicketCollectionTransactionTickets_FinancialItemType_SourceRecordId] ON [TicketCollectionTransactionTickets] ([FinancialItemType], [SourceRecordId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TicketCollectionTransactionTickets_TicketCollectionTransactionId_Source_SourceRecordId] ON [TicketCollectionTransactionTickets] ([TicketCollectionTransactionId], [Source], [SourceRecordId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    CREATE INDEX [IX_TicketCollectionTransactionTickets_TicketIdSnapshot] ON [TicketCollectionTransactionTickets] ([TicketIdSnapshot]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GateOperations_CommandId] ON [GateOperations] ([CommandId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    CREATE INDEX [IX_GateOperations_ControllerId] ON [GateOperations] ([ControllerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_GateOperations_ReservedControllerId] ON [GateOperations] ([ReservedControllerId]) WHERE [ReservedControllerId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_GateOperations_ReservedTicketId] ON [GateOperations] ([ReservedTicketId]) WHERE [ReservedTicketId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    CREATE INDEX [IX_GateOperations_Status_CreatedAtUtc] ON [GateOperations] ([Status], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    CREATE INDEX [IX_GateOperations_TicketIdSnapshot_Type] ON [GateOperations] ([TicketIdSnapshot], [Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    CREATE INDEX [IX_OvernightPenaltyCoverages_PaymentId] ON [OvernightPenaltyCoverages] ([PaymentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124122834_ParkingFinancialIntegrityV2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261124122834_ParkingFinancialIntegrityV2', N'9.0.10');
END;

COMMIT;
GO

