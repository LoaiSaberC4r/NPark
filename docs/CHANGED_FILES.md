# File inventory

## Added

- `NPark.Api/Contracts/GateOperationHttp.cs` — API, gate authorization, configuration or SignalR
- `NPark.Application/Feature/TicketsManagement/Command/Add/PersistIssuedTicket.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Options/ParkingWorkflowOptions.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Services/FinancialSettlementService.cs` — Payment, pricing or administrative settlement
- `NPark.Application/Services/GateOperationService.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Services/GateOperationTransitions.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Services/GatePacketCodec.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Services/OvernightPenaltyPolicy.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Services/ParkingBusinessClock.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Services/TicketArchiveService.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Services/TicketExitEligibilityService.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Services/TicketSanctionsSpec.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Services/WorkflowMessages.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Domain/Entities/GateOperation.cs` — Persistent domain state or localized workflow errors
- `NPark.Domain/Entities/OvernightPenaltyCoverage.cs` — Persistent domain state or localized workflow errors
- `NPark.Domain/Enums/TicketLifecycle.cs` — Persistent domain state or localized workflow errors
- `NPark.Infrastructure/Configuration/ParkingWorkflowConfiguration.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Infrastructure/Migrations/20261124122834_ParkingFinancialIntegrityV2.Designer.cs` — SQL Server schema, historical backfill or deployment script
- `NPark.Infrastructure/Migrations/20261124122834_ParkingFinancialIntegrityV2.cs` — SQL Server schema, historical backfill or deployment script
- `NPark.Infrastructure/NParkDesignTimeDbContextFactory.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Infrastructure/Services/Mqtt/GateAckSubscriber.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Infrastructure/Services/Mqtt/GateOperationRecoveryService.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Infrastructure/Services/TransactionCommitVerifier.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Tests/GateProtocolTests.cs` — Automated protocol, migration or workflow verification
- `NPark.Tests/LegacyMigrationTests.cs` — Automated protocol, migration or workflow verification
- `NPark.Tests/NPark.Tests.csproj` — Automated protocol, migration or workflow verification
- `NPark.Tests/ParkingWorkflowIntegrationTests.cs` — Automated protocol, migration or workflow verification
- `docs/API_CONTRACT_CHANGES.md` — Integration, release or verification documentation
- `docs/CHANGED_FILES.md` — Integration, release or verification documentation
- `docs/DATABASE_MIGRATION.md` — Integration, release or verification documentation
- `docs/FINANCIAL_SETTLEMENT.md` — Integration, release or verification documentation
- `docs/FRONTEND_INTEGRATION.md` — Integration, release or verification documentation
- `docs/MQTT_GATE_PROTOCOL.md` — Integration, release or verification documentation
- `docs/NPARK_FINANCIAL_INTEGRITY_V2.sql` — SQL Server schema, historical backfill or deployment script
- `docs/NPARK_FINANCIAL_INTEGRITY_V2_PREFLIGHT.sql` — SQL Server schema, historical backfill or deployment script
- `docs/NPARK_REFACTORING_V2.md` — Integration, release or verification documentation
- `docs/TEST_RESULTS.md` — Integration, release or verification documentation

## Modified

- `BuildingBlock.Api/ProblemDetailsMapping.cs` — Shared result, transaction, repository, cache or media behavior
- `BuildingBlock.Application/Abstraction/IMediatorCommandQuery.cs` — Shared result, transaction, repository, cache or media behavior
- `BuildingBlock.Application/Abstraction/Media/IMediaService.cs` — Shared result, transaction, repository, cache or media behavior
- `BuildingBlock.Application/Behaviors/CacheBehavior.cs` — Shared result, transaction, repository, cache or media behavior
- `BuildingBlock.Application/Behaviors/ExceptionMappingBehavior.cs` — Shared result, transaction, repository, cache or media behavior
- `BuildingBlock.Application/Behaviors/ResilienceBehavior.cs` — Shared result, transaction, repository, cache or media behavior
- `BuildingBlock.Application/Behaviors/TransactionBehavior.cs` — Shared result, transaction, repository, cache or media behavior
- `BuildingBlock.Domain/Results/Result.cs` — Shared result, transaction, repository, cache or media behavior
- `BuildingBlock.Infrastracture/Bootstrap/DependencyInjection.cs` — Shared result, transaction, repository, cache or media behavior
- `BuildingBlock.Infrastracture/Repositories/EfUnitOfWork.cs` — Shared result, transaction, repository, cache or media behavior
- `BuildingBlock.Infrastracture/Service/MediaService.cs` — Shared result, transaction, repository, cache or media behavior
- `NPark.Api/Contracts/OpenGateRequest.cs` — API, gate authorization, configuration or SignalR
- `NPark.Api/Controllers/GateController.cs` — API, gate authorization, configuration or SignalR
- `NPark.Api/Controllers/TicketController.cs` — API, gate authorization, configuration or SignalR
- `NPark.Api/RealTime/NotificationsHub.cs` — API, gate authorization, configuration or SignalR
- `NPark.Api/RealTime/RealtimeNotifier.cs` — API, gate authorization, configuration or SignalR
- `NPark.Api/appsettings.json` — API, gate authorization, configuration or SignalR
- `NPark.Application/Abstraction/IRealtimeNotificationClient.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Abstraction/IRealtimeNotifier.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Bootstrap/Bootstrap.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/GateManagement/Command/OpenGate/OpenGateCommand.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/GateManagement/Command/OpenGate/OpenGateCommandHandler.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/MqttNetworkManagement/MqttCaptureDetectedResponse.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/MqttNetworkManagement/ProcessMqttExitPacket/ProcessMqttExitPacketCommand.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/MqttNetworkManagement/ProcessMqttExitPacket/ProcessMqttExitPacketCommandHandler.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/Reports/Query/GetEntranceCashierTicketsReport/EntranceCashierTicketsReportResponse.cs` — Supervisor or operational reporting
- `NPark.Application/Feature/Reports/Query/GetEntranceCashierTicketsReport/GetEntranceCashierInsideTicketsSpec.cs` — Supervisor or operational reporting
- `NPark.Application/Feature/Reports/Query/GetEntranceCashierTicketsReport/GetEntranceCashierTicketsReportQueryHandler.cs` — Supervisor or operational reporting
- `NPark.Application/Feature/Reports/Query/GetSupervisorCollectionReport/GetSupervisorCollectionReportQuery.cs` — Supervisor or operational reporting
- `NPark.Application/Feature/Reports/Query/GetSupervisorCollectionReport/GetSupervisorCollectionReportQueryHandler.cs` — Supervisor or operational reporting
- `NPark.Application/Feature/Reports/Query/GetSupervisorCollectionsReport/GetActiveTicketCollectionsSpec.cs` — Supervisor or operational reporting
- `NPark.Application/Feature/Reports/Query/GetSupervisorCollectionsReport/GetArchivedTicketCollectionsSpec.cs` — Supervisor or operational reporting
- `NPark.Application/Feature/Reports/Query/GetSupervisorCollectionsReport/GetPendingExceedCollectionsSpec.cs` — Supervisor or operational reporting
- `NPark.Application/Feature/Reports/Query/GetSupervisorCollectionsReport/GetSupervisorCollectionsReportQueryHandler.cs` — Supervisor or operational reporting
- `NPark.Application/Feature/Reports/Query/GetSupervisorCollectionsReport/GetSupervisorExceedCollectionsSpec.cs` — Supervisor or operational reporting
- `NPark.Application/Feature/Reports/Query/GetSupervisorCollectionsReport/SupervisorCollectionsReportResponse.cs` — Supervisor or operational reporting
- `NPark.Application/Feature/TicketExceedTransactionsManagement/Command/CollectTicketExceedTransaction/CollectTicketExceedTransactionCommand.cs` — Payment, pricing or administrative settlement
- `NPark.Application/Feature/TicketExceedTransactionsManagement/Command/CollectTicketExceedTransaction/CollectTicketExceedTransactionCommandHandler.cs` — Payment, pricing or administrative settlement
- `NPark.Application/Feature/TicketExceedTransactionsManagement/Command/CollectTicketExceedTransaction/UncollectedTicketExceedTransactionsByGateSpec.cs` — Payment, pricing or administrative settlement
- `NPark.Application/Feature/TicketExceedTransactionsManagement/Command/Create/CreateTicketExceedTransactionCommand.cs` — Payment, pricing or administrative settlement
- `NPark.Application/Feature/TicketExceedTransactionsManagement/Command/Create/CreateTicketExceedTransactionCommandHandler.cs` — Payment, pricing or administrative settlement
- `NPark.Application/Feature/TicketExceedTransactionsManagement/Command/Create/CreateTicketExceedTransactionCommandValidation.cs` — Payment, pricing or administrative settlement
- `NPark.Application/Feature/TicketsManagement/Command/Add/AddTicketCommandHandler.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/TicketsManagement/Command/Add/AddTicketCommandResponse.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/TicketsManagement/Command/CalculateSalary/CalculateSalaryCommand.cs` — Payment, pricing or administrative settlement
- `NPark.Application/Feature/TicketsManagement/Command/CalculateSalary/CalculateSalaryCommandHandler.cs` — Payment, pricing or administrative settlement
- `NPark.Application/Feature/TicketsManagement/Command/CalculateSalary/CalculateSalaryCommandResponse.cs` — Payment, pricing or administrative settlement
- `NPark.Application/Feature/TicketsManagement/Command/CalculateSalaryForSubscriber/CalculateSalaryForSubscriberCommandHandler.cs` — Payment, pricing or administrative settlement
- `NPark.Application/Feature/TicketsManagement/Command/CollectDailyTicket/CollectDailyTicketCommandHandler.cs` — Payment, pricing or administrative settlement
- `NPark.Application/Feature/TicketsManagement/Command/CollectDailyTicket/CollectDailyTicketResponse.cs` — Payment, pricing or administrative settlement
- `NPark.Application/Feature/TicketsManagement/Command/CollectEnterTickets/CollectEnterTicketsCommand.cs` — Payment, pricing or administrative settlement
- `NPark.Application/Feature/TicketsManagement/Command/CollectEnterTickets/CollectEnterTicketsCommandHandler.cs` — Payment, pricing or administrative settlement
- `NPark.Application/Feature/TicketsManagement/Command/CollectEnterTickets/UncollectedArchivedEnterTicketsByGateSpec.cs` — Payment, pricing or administrative settlement
- `NPark.Application/Feature/TicketsManagement/Command/CollectEnterTickets/UncollectedEnterTicketsByGateSpec.cs` — Payment, pricing or administrative settlement
- `NPark.Application/Feature/TicketsManagement/Command/ColletByCachier/ColletByCachierCommand.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/TicketsManagement/Command/ColletByCachier/ColletByCachierCommandHandler.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/TicketsManagement/Command/ExitTicket/ExitTicketCommand.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/TicketsManagement/Command/ExitTicket/ExitTicketCommandHandler.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/TicketsManagement/Command/ScanQrCode/ScanQrCodeCommandHandler.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/TicketsManagement/Command/ValidateExitByText/ValidateExitByTextCommand.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/TicketsManagement/Command/ValidateExitByText/ValidateExitByTextCommandHandler.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/TicketsManagement/Command/ValidateExitByText/ValidateExitByTextCommandResponse.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/TicketsManagement/Query/GetEntryTicketForAdmin/GetEntryTicketForAdminQueryHandler.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/TicketsManagement/Query/GetExitTicketForAdmin/GetExitTicketForAdminQueryHandler.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/TicketsManagement/Query/GetExitTicketForAdmin/GetExitTicketForAdminQueryResponse.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/TicketsManagement/Query/GetTicketForEntryGate/GetTicketForEntryGateQueryHandler.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/TicketsManagement/Query/GetTicketForEntryGate/GetTicketForEntryGateQueryResponse.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/TicketsManagement/Query/GetTicketForExitGate/GetTicketForExitGateQueryHandler.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/TicketsManagement/Query/GetTicketInsidePark/GetTicketInsideParkQueryHandler.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/TicketsManagement/Query/ValidateExitByVehicleNumber/ValidateExitByVehicleNumberQueryHandler.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Feature/TicketsManagement/Query/ValidateExitByVehicleNumber/ValidateExitByVehicleNumberQueryResponse.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Specifications/TicketSpecification/GetAllTicketsEntryForTodaySpecification.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Specifications/TicketSpecification/GetAllTicketsExitForTodaySpecification.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Specifications/TicketSpecification/GetTicketInsideParkSpecification.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Specifications/TicketSpecification/TicketsCreatedTodayForEntryGateSpec.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Application/Specifications/TicketSpecification/TicketsCreatedTodayForExitGateSpec.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Domain/Entities/Ticket.cs` — Persistent domain state or localized workflow errors
- `NPark.Domain/Entities/TicketArchive.cs` — Persistent domain state or localized workflow errors
- `NPark.Domain/Entities/TicketCollectionTransactionLine.cs` — Persistent domain state or localized workflow errors
- `NPark.Domain/Entities/TicketCollectionTransactionTicket.cs` — Persistent domain state or localized workflow errors
- `NPark.Domain/Entities/TicketExceedTransaction.cs` — Persistent domain state or localized workflow errors
- `NPark.Domain/Enums/TicketCollectionItemSource.cs` — Persistent domain state or localized workflow errors
- `NPark.Domain/Enums/TicketCollectionTransactionType.cs` — Persistent domain state or localized workflow errors
- `NPark.Domain/Resource/ErrorMessage.ar.resx` — Persistent domain state or localized workflow errors
- `NPark.Domain/Resource/ErrorMessage.resx` — Persistent domain state or localized workflow errors
- `NPark.Infrastructure/Bootstrap/Bootstrap.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Infrastructure/Configuration/TicketArchiveConfiguration.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Infrastructure/Configuration/TicketCollectionTransactionLineConfiguration.cs` — Payment, pricing or administrative settlement
- `NPark.Infrastructure/Configuration/TicketCollectionTransactionTicketConfiguration.cs` — Payment, pricing or administrative settlement
- `NPark.Infrastructure/Configuration/TicketConfiguration.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Infrastructure/Configuration/TicketExceedTransactionConfiguration.cs` — Payment, pricing or administrative settlement
- `NPark.Infrastructure/Migrations/NParkDBContextModelSnapshot.cs` — SQL Server schema, historical backfill or deployment script
- `NPark.Infrastructure/NParkDBContext.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Infrastructure/Services/DashboardService.cs` — Supervisor or operational reporting
- `NPark.Infrastructure/Services/Mqtt/MqttNetworkSubscriberService.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.Infrastructure/Services/MqttPublisher.cs` — Unified ticket lifecycle, gate correlation or recovery
- `NPark.sln` — Register the test project

## Removed

- `NPark.Application/DomaineEventHandler/ExitTicketDomainEventHandler.cs` — Replaced by the shared eligibility or acknowledged exit workflow
- `NPark.Application/Feature/TicketsManagement/Command/ExitTicketFromMqtt/ExitTicketFromMqttCommand/ExitTicketFromMqttCommand.cs` — Replaced by the shared eligibility or acknowledged exit workflow
- `NPark.Application/Feature/TicketsManagement/Command/ExitTicketFromMqtt/ExitTicketFromMqttCommand/ExitTicketFromMqttCommandHandler.cs` — Replaced by the shared eligibility or acknowledged exit workflow
- `NPark.Application/Feature/TicketsManagement/Command/ExitTicketFromMqtt/ExitTicketFromMqttCommand/ExitTicketFromMqttCommandValidator.cs` — Replaced by the shared eligibility or acknowledged exit workflow
- `NPark.Application/Feature/TicketsManagement/Query/ValidateExitByVehicleNumber/GetTodayTicketByVehicleNumberSpec.cs` — Replaced by the shared eligibility or acknowledged exit workflow
- `NPark.Domain/DomainEvent/ExitTicketDomainEvent.cs` — Replaced by the shared eligibility or acknowledged exit workflow

## Overnight penalty follow-up

### Added

- `NPark.Application/Services/SanctionCatalogSelection.cs`
- `NPark.Tests/OvernightPenaltyIntegrationTests.cs`

### Modified

- `BuildingBlock.Domain/Exceptions/ExceptionToErrorMapper.cs`
- `NPark.Api/appsettings.json`
- `NPark.Application/Bootstrap/Bootstrap.cs`
- `NPark.Application/Feature/TicketExceedTransactionsManagement/Command/Create/CreateTicketExceedTransactionCommand.cs`
- `NPark.Application/Feature/TicketExceedTransactionsManagement/Command/Create/CreateTicketExceedTransactionCommandHandler.cs`
- `NPark.Application/Feature/TicketExceedTransactionsManagement/Command/Create/CreateTicketExceedTransactionCommandValidation.cs`
- `NPark.Application/Feature/TicketsManagement/Command/CalculateSalary/CalculateSalaryCommandHandler.cs`
- `NPark.Application/Feature/TicketsManagement/Command/CalculateSalaryForSubscriber/CalculateSalaryForSubscriberCommand.cs`
- `NPark.Application/Feature/TicketsManagement/Command/CalculateSalaryForSubscriber/CalculateSalaryForSubscriberCommandHandler.cs`
- `NPark.Application/Feature/TicketsManagement/Command/ValidateExitByText/TicketByCardNumberSpecification.cs`
- `NPark.Application/Feature/TicketsManagement/Command/ValidateExitByText/ValidateExitByTextCommandHandler.cs`
- `NPark.Application/Options/ParkingWorkflowOptions.cs`
- `NPark.Application/Services/OvernightPenaltyPolicy.cs`
- `NPark.Application/Services/TicketExitEligibilityService.cs`
- `NPark.Domain/Entities/TicketExceedTransaction.cs`
- `NPark.Domain/Resource/ErrorMessage.ar.resx`
- `NPark.Domain/Resource/ErrorMessage.resx`
- `NPark.Infrastructure/Bootstrap/Bootstrap.cs`
- `NPark.Tests/ParkingWorkflowIntegrationTests.cs`
- `docs/API_CONTRACT_CHANGES.md`
- `docs/CHANGED_FILES.md`
- `docs/FINANCIAL_SETTLEMENT.md`
- `docs/FRONTEND_INTEGRATION.md`
- `docs/NPARK_REFACTORING_V2.md`
- `docs/TEST_RESULTS.md`

### Removed

The configured overnight sanction, policy mode, and obsolete eligibility reason were removed from the existing source and configuration files. This follow-up removed no files.

## NPARK-MQTT-001-002

Both repositories use `codex/mqtt-v111-entrance`. The backend starts from `a5ceb15b840180f8a55bd69652d185309dd8ca30`, and Angular starts from `3e78f72e6200a5a24ac3245a08d2dd6b4c65f2cc`.

### NPark backend

| Full path | Change | Purpose |
|---|---|---|
| `E:/NPark/ITC/NPark.Api/RealTime/RealtimeNotifier.cs` | Modified | Sends automatic TicketAdded events to the entrance gate's group. |
| `E:/NPark/ITC/NPark.Api/appsettings.json` | Modified | Defines LPR retention and disabled defaults for the new hardware features. |
| `E:/NPark/ITC/NPark.Application/Bootstrap/Bootstrap.cs` | Modified | Registers the shared subscriber and ticket services. |
| `E:/NPark/ITC/NPark.Application/Feature/LprSignalRManagement/Command/VehicleDetected/LprVehicleDetectedCommandHandler.cs` | Modified | Stores accepted captures, checks the camera source, and waits for Lock before notifying. |
| `E:/NPark/ITC/NPark.Application/Feature/TicketsManagement/Command/Add/AddTicketCommandHandler.cs` | Modified | Shares subscriber eligibility, visitor ticket construction, and ticket notifications. |
| `E:/NPark/ITC/NPark.Application/Feature/TicketsManagement/Command/Add/IssueAutomaticVisitorTicket.cs` | Added | Validates the event and creates the ticket and capture association in a serializable transaction. |
| `E:/NPark/ITC/NPark.Application/Options/EntranceIntegrationOptions.cs` | Added | Defines LPR, Lock, and automatic visitor options. |
| `E:/NPark/ITC/NPark.Application/Services/GateOperationService.cs` | Modified | Authorizes and correlates ticketless B2 and automatic B1 operations. |
| `E:/NPark/ITC/NPark.Application/Services/GatePacketCodec.cs` | Modified | Encodes B2 and selects the wire opcode by operation type. |
| `E:/NPark/ITC/NPark.Application/Services/ParkingBusinessClock.cs` | Modified | Safely converts an unambiguous firmware wall-clock timestamp. |
| `E:/NPark/ITC/NPark.Application/Services/ParkingEventPacketCodec.cs` | Added | Validates Schema 01 and 02 frames and encodes the storage ACK. |
| `E:/NPark/ITC/NPark.Application/Services/SubscriberEligibilityService.cs` | Added | Uses existing date and active-ticket rules for subscriber eligibility. |
| `E:/NPark/ITC/NPark.Application/Services/TicketIssuanceService.cs` | Added | Shares visitor initialization and TicketAdded notification construction. |
| `E:/NPark/ITC/NPark.Application/Shared/Dto/TicketAddedNotification.cs` | Modified | Adds the automatic issuance indicator. |
| `E:/NPark/ITC/NPark.Domain/Entities/GateOperation.cs` | Modified | Persists the automatic visitor operation marker. |
| `E:/NPark/ITC/NPark.Domain/Entities/LprCapture.cs` | Added | Models a received detection and guards its one-ticket association. |
| `E:/NPark/ITC/NPark.Domain/Entities/LprCaptureArchive.cs` | Added | Preserves the identity and history of an archived detection. |
| `E:/NPark/ITC/NPark.Domain/Entities/ParkingHardwareEvent.cs` | Added | Stores binary event identity, evidence, and durable progress. |
| `E:/NPark/ITC/NPark.Domain/Entities/Ticket.cs` | Modified | Assigns a visitor card while preserving the visitor's pricing and ticket state. |
| `E:/NPark/ITC/NPark.Domain/Enums/TicketLifecycle.cs` | Modified | Adds LockCardButton as persisted operation type 4. |
| `E:/NPark/ITC/NPark.Infrastructure/Bootstrap/Bootstrap.cs` | Modified | Validates options on startup and registers the archive and MQTT workers. |
| `E:/NPark/ITC/NPark.Infrastructure/Configuration/ParkingEntranceConfiguration.cs` | Added | Maps the entrance tables, indexes, and constraints. |
| `E:/NPark/ITC/NPark.Infrastructure/Migrations/20261125135033_AddLprAndParkingEventIntegration.cs` | Added | Creates the entrance tables and adds the visitor flag to GateOperations. |
| `E:/NPark/ITC/NPark.Infrastructure/Migrations/20261125135033_AddLprAndParkingEventIntegration.Designer.cs` | Added | Records the generated EF migration target model. |
| `E:/NPark/ITC/NPark.Infrastructure/Migrations/NParkDBContextModelSnapshot.cs` | Modified | Records the current EF model. |
| `E:/NPark/ITC/NPark.Infrastructure/NParkDBContext.cs` | Modified | Exposes the three new table sets. |
| `E:/NPark/ITC/NPark.Infrastructure/Options/MqttParkingEventsOptions.cs` | Added | Defines event subscription enablement and QoS settings. |
| `E:/NPark/ITC/NPark.Infrastructure/Services/Lpr/LprCaptureArchiveService.cs` | Added | Moves expired captures to the archive in bounded SQL transactions. |
| `E:/NPark/ITC/NPark.Infrastructure/Services/Mqtt/GateOperationRecoveryService.cs` | Modified | Applies feature and controller allowlists to pending automatic dispatch. |
| `E:/NPark/ITC/NPark.Infrastructure/Services/Mqtt/MqttParkingEventSubscriber.cs` | Added | Maintains the separate parking MQTT subscription and bounded binary intake. |
| `E:/NPark/ITC/NPark.Infrastructure/Services/Mqtt/ParkingEventProcessingService.cs` | Added | Recovers acknowledged events, authorizes B1, and records operation outcomes. |
| `E:/NPark/ITC/NPark.Infrastructure/Services/Mqtt/ParkingEventStorage.cs` | Added | Commits event data, checks replays, and publishes storage acknowledgments. |
| `E:/NPark/ITC/NPark.Tests/EntranceMqttIntegrationTests.cs` | Added | Exercises entrance processing with disposable SQL and MQTT simulation. |
| `E:/NPark/ITC/NPark.Tests/DatabaseStartupTests.cs` | Modified | Checks the full pending migration list on startup, cancellation, and disabled startup. |
| `E:/NPark/ITC/NPark.Tests/MqttGateIntegrationTests.cs` | Modified | Captures archive worker diagnostics in the SQL and MQTT fixture. |
| `E:/NPark/ITC/NPark.Tests/ParkingEventProtocolTests.cs` | Added | Verifies binary encoding and strict frame decoding. |
| `E:/NPark/ITC/NPark.Tests/ParkingWorkflowIntegrationTests.cs` | Modified | Supports entrance test configuration and records notification payloads. |
| `E:/NPark/ITC/docs/API_CONTRACT_CHANGES.md` | Modified | Documents additive HTTP and SignalR behavior. |
| `E:/NPark/ITC/docs/CHANGED_FILES.md` | Modified | Lists the changed files for both feature branches. |
| `E:/NPark/ITC/docs/FRONTEND_INTEGRATION.md` | Modified | Documents operation normalization and the passive ticket refresh. |
| `E:/NPark/ITC/docs/MQTT_GATE_PROTOCOL.md` | Modified | Documents wire frames, recovery, configuration, and deployment limits. |
| `E:/NPark/ITC/docs/TEST_RESULTS.md` | Modified | Records executed checks and outstanding verification. |

### NParkAngular

| Full path | Change | Purpose |
|---|---|---|
| `E:/NPark/Npark/NParkAngular/src/app/Domain/Gate/gate-operation.models.ts` | Modified | Normalizes LockCardButton, terminal status, and automatic entry metadata. |
| `E:/NPark/Npark/NParkAngular/src/app/Domain/Gate/gate-operation.models.spec.ts` | Added | Tests numeric and string Lock normalization and terminal SignalR behavior. |
| `E:/NPark/Npark/NParkAngular/src/app/Domain/OverView/dashboard-notifications.model.ts` | Modified | Models the automatic TicketAdded flag and ticket identity. |
| `E:/NPark/Npark/NParkAngular/src/app/Feature/Entry-Component/ticket-component.ts` | Modified | Refreshes same-gate automatic tickets without triggering a cashier action. |
