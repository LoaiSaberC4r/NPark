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
