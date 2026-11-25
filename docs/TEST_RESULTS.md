# Verification

The solution targets .NET 8 and uses the existing EF Core SQL Server stack. Verification used a local SQL Server Express instance with disposable databases and a dedicated loopback Mosquitto listener. No production database or physical gate was used.

## Results

| Check | Result |
|---|---|
| Full solution build | Passed with 27 warnings and zero errors. The baseline also reported dependency and compiler warnings. |
| Automated tests | 19 passed, zero failed, zero skipped with SQL Server and Mosquitto enabled. |
| EF migration model | No pending model changes. |
| Baseline upgrade and SQL rerun | Upgrade applied and a second run succeeded. All three financial identity and gate reservation unique indexes were inspected. |
| Migration with legacy rows | Existing dates, ticket and payment IDs, sanction links, and collection amounts were preserved. Duplicate history stopped the migration and rolled it back. |
| Whitespace check | Passed. |

## Commands

- `dotnet build NPark.sln --no-restore --nologo -clp:ErrorsOnly`
- `dotnet test NPark.sln --no-restore --nologo --logger "console;verbosity=minimal" -clp:ErrorsOnly`
- `dotnet ef migrations has-pending-model-changes --project NPark.Infrastructure --startup-project NPark.Api --context NParkDBContext --no-build`
- `sqlcmd -b -S .\SQLEXPRESS -d NParkV2Integration -E -C -i docs/NPARK_FINANCIAL_INTEGRITY_V2.sql`, run twice against the same isolated database
- `git diff --check`

Set `NPARK_TEST_SQL` to the isolated SQL Server connection and `NPARK_TEST_MQTT_PORT` to the dedicated loopback broker listener to enable integration tests. With those variables absent, hardware-independent protocol tests run and the applicable integration tests are skipped.

## Coverage

- Gate packet encoding, packet and topic validation, ACK status and correlation, and publish/ACK ordering.
- Real SQL Server rollback on failure results, exceptions, and ignored nested failures.
- Legacy upgrade with representative data, including original entry timestamp, sanction links, source ID backfill, and duplicate detection that rolls back the migration.
- Same-ticket HTTP and QR requests share an exit operation. The simulator verifies raw MQTT dispatch, timeout with the ticket still active, duplicate and late ACK handling, atomic archive failure and recovery, and attachment and settlement preservation.
- Independent ordinary and overnight charges, stable QR identity, repeated request behavior, and separate supervisor collections for multiple sanctions.
- Issuance, correlated entry and exit, Exit Pricing quote and payment, actual exit-cashier attribution, daily settlement snapshots, and supervisor-specific reports.
- The API contract test invokes the ticket controller and verifies HTTP 202, operation location, and pending response flags.

Physical firmware, full Angular end-to-end behavior, restart and broker-outage drills, uncertain SQL commit fault injection, and the full acceptance matrix require further verification before production activation.

## Overnight penalty follow-up

| Check | Result |
|---|---|
| `dotnet build NPark.sln --nologo -clp:ErrorsOnly` | Passed with 83 warnings and zero errors. |
| `dotnet test NPark.sln --nologo --logger "console;verbosity=normal" -clp:ErrorsOnly` | 34 passed, zero failed, zero skipped with isolated SQL Server and MQTT enabled. |
| EF migration model | No pending model changes. |

The dated exit scenario ran against an isolated SQL Server Express database and a loopback Mosquitto simulator. The first ticket had a November 23 start date and a confirmed base fee of 20. On November 24, HTTP and MQTT QR validation rejected the overdue ticket. A catalog payment of 50 renewed validity through November 24. At Cairo midnight, replaying the same payment returned its original ID and left validity unchanged. A separate November 25 payment reused the same catalog item and brought the ticket total to 120.

The simulator received one correlated open command. A timeout left the ticket active. A late success ACK survived an injected archive failure, and recovery archived the ticket once using the original ACK. Ticket ID, QR, entry date, and attachments survived. After exit, the supervisor settled both independent sanctions for 100. The base payment remained a separate 20, and the financial snapshots retained their individual source IDs.

Additional relational tests exercised an empty sanction catalog, obsolete deployment settings, subscriber issuance and renewal, changed catalog amounts, same-day ordinary payment, concurrent renewal, and rollback at payment insert, coverage insert, ticket update, and duplicate coverage. The EF model and existing migrations were unchanged.

## MQTT dispatch and acknowledgment follow-up

The previous Development and Testing settings inherited `192.168.1.14` from the base configuration. A diagnostic API instance in the Testing environment used `192.168.1.40:1883` and received a QoS 1 grant for `ack/network/#`. Its isolated database contained zero gates and zero operations. Separate MQTT monitors saw heartbeat frames on `sub/network/1` and `sub/network/5` during the passive observation window. The physical controller exit requires a verified safe test setup. IIS deployment overrides could not be read with the available Windows permissions.

The authenticated HTTP integration test reproduces a timeout on Controller 5, reassigns the gate to Controller 1, and confirms that the next exit request returns HTTP 409. The original SQL row keeps its command ID, controller, reservations, and publication timestamp. After supervisor reconciliation, the API authorizes a fresh command, which the recovery worker sends through loopback Mosquitto. The broker outage test uses a separate loopback MQTT broker to exercise subscription rejection, retained ACK rejection, pending recovery after an API restart, and manual dispatch after reconnect.

| Isolated Controller 1 wire evidence | Hex |
|---|---|
| Backend command on `sub/network/1`, 16 bytes, QoS 1, retain disabled | `7B00010000000000000002AEBEC1B17D` |
| Simulator ACK on `ack/network/1`, 13 bytes, status `00` | `7B00010000000000000002007D` |

Both frames carry command ID `2`. An induced SQL ACK write failure left the operation `Published` and the archive empty. The persistent session redelivered the ACK after the failure cleared. SQL then recorded ACK status `00`, the acknowledgment and completion timestamps, one archive row with the original ticket identity and base price, and released reservations. Duplicate ACKs caused no additional archive or outbound command. The existing late-ACK regression also verified recovery from an archive failure without another gate command.

The recorded isolated operation was `5739a08c-9eaf-4104-b9f4-714b1b3cb4d6` for ticket `dcc2aefc-2b32-42a3-88cc-6e76b569b568`. The fixed test clock put dispatch, publication, ACK persistence, and completion at `2026-11-24T10:00:03Z`. The assertions checked the database and HTTP state at each stage.

On November 24, 2026, `dotnet build NPark.sln` succeeded with 40 warnings and zero errors. `dotnet test NPark.sln` passed all 36 tests, with zero failures or skips, using disposable SQL Server Express databases and loopback MQTT brokers.

## Automatic controller release, November 25, 2026

Branch `codex/automatic-controller-release` starts from master at `3116baf8ce49a3bf5b077553ca82c581975cff0c`. Previously, uncertain operations kept the filtered controller and ticket reservations, and a late ACK could resume the operation. The current lifecycle makes those outcomes terminal. It permits a fresh authorized operation and keeps the earlier attempt in the audit history.

The authenticated API test used disposable SQL Server Express databases and an isolated Mosquitto listener on `127.0.0.1:18883`. Ticket `5d9aa415-e9bb-4fd9-ac4f-a7425a0a5420` had a base price of 20, a confirmed sanction of 400, a paid amount of 420, zero outstanding, and current business-day validity in the test clock. Revalidation after timeout returned `Eligible` with the same balance and validity. The test replayed the first HTTP request ID and received its original result. A fresh UUID created command 2 after restart. It reused the ticket and controller without reconciliation. SQL contained one archive row at the end.

| SQL checkpoint | Status | Reserved controller | Reserved ticket |
|---|---|---|---|
| Command 1 before expiry | `Published` | `1` | `5d9aa415-e9bb-4fd9-ac4f-a7425a0a5420` |
| Same operation after expiry | `TimedOut` | `NULL` | `NULL` |
| Command 2 after matching ACK and archival | `Completed` | `NULL` | `NULL` |

The command 1 operation ID was `17fc7487-e5a3-4a6a-a883-eb3dffe81768`. The ticket stayed active through command 1's timeout. The simulator sent both a late success ACK and a late failure ACK for command 1 while command 2 was published. Command 2 kept its reservations until its own ACK arrived.

| Observed frame on the isolated broker | Topic | Payload hex |
|---|---|---|
| First backend open command | `sub/network/1` | `7B00010000000000000001AEBEC1B17D` |
| Fresh backend open command | `sub/network/1` | `7B00010000000000000002AEBEC1B17D` |
| Ignored success ACK from the simulator | `ack/network/1` | `7B00010000000000000001007D` |
| Accepted success ACK from the simulator | `ack/network/1` | `7B00010000000000000002007D` |

The overnight regression used controller `54321` to exercise separate tickets on the same controller. One ticket completed after another ticket timed out. During an injected archive write failure, command 4 stayed `Acknowledged` with controller `NULL` and ticket `b6b533b3-d7e2-436a-89ff-10e850ad0546` still reserved. Another ticket completed on that controller before the constraint was removed. The original ticket then archived once with the same ACK timestamp and no additional MQTT command. The existing sanction, settlement, and QR assertions passed.

The deadline test checked the SQL state immediately before and at equality, including competing ACK and timeout transactions. A controlled publish canceled after its deadline retained the reservations until it settled, then committed `Unknown` and released both reservations despite an injected SignalR exception. The restart and broker outage tests exercised a subscriber without an ACK subscription grant, pending recovery before expiry, dispatch expiry during disconnection, and publication of a fresh command after reconnection. Entry and manual tests covered successive failures, the current gate mapping, and failure ACK codes 01, 02, and 03.

The startup test seeded historical `TimedOut`, `Unknown`, `Failed`, `Completed`, and `Acknowledged` rows with reservations. Before any worker started, SQL showed both reservations cleared on terminal rows and only the controller cleared on the acknowledged row. An active pending row kept both reservations. Historical command IDs, gate mappings, ticket snapshots, publication and ACK timestamps, and error codes survived. A second startup left the row versions unchanged. EF reported no pending model changes.

The configured test broker at `192.168.1.40:1883` accepted the TCP probe and passive MQTT subscriptions. The observation window showed the heartbeat `7BFFA31A2A3A4A7D` on `sub/network/1` and `sub/network/5`. The open and ACK evidence above is from loopback. Physical-controller execution remains unverified pending an authorized and physically safe setup.

### Changed files

| Area | Files |
|---|---|
| Domain and localization | `GateOperation.cs`, `ErrorMessage.resx`, `ErrorMessage.ar.resx` |
| Application | `GateOperationTransitions.cs`, `GateOperationService.cs`, `TicketExitEligibilityService.cs`, `ParkingWorkflowOptions.cs`, `IMqttPublisher.cs`, `OpenGateCommand.cs`, `OpenGateCommandHandler.cs`, `ExitTicketCommand.cs`, `ExitTicketCommandHandler.cs` |
| API | `GateController.cs`, `GateOperationHttp.cs`, `OpenGateRequest.cs`, `appsettings.json` |
| Infrastructure | `Bootstrap.cs`, `GateOperationRecoveryService.cs`, `GateAckSubscriber.cs`, `MqttPublisher.cs` |
| Tests | `DatabaseStartupTests.cs`, `GateProtocolTests.cs`, `GateLifecycleIntegrationTests.cs`, `MqttGateIntegrationTests.cs`, `ParkingWorkflowIntegrationTests.cs` |
| Documentation | `FRONTEND_INTEGRATION.md`, `API_CONTRACT_CHANGES.md`, `MQTT_GATE_PROTOCOL.md`, `TEST_RESULTS.md` |

### Final checks

| Check | Result |
|---|---|
| `dotnet build NPark.sln` | Passed with 42 warnings and zero errors. |
| `dotnet test NPark.sln` with `NPARK_TEST_SQL` and `NPARK_TEST_MQTT_PORT` | 47 passed, zero failed, zero skipped. |
| `dotnet ef migrations has-pending-model-changes` | No changes since the last migration. |
| `git diff --check` | Passed. |

## MQTT v1.1.11 entrance verification

The entrance tests use disposable SQL Server Express databases, an HTTP API test host, and a loopback-only Mosquitto listener. The MQTT simulator publishes Schema 01 and Schema 02 events and returns synthetic command ACKs. The tests exercise subscriber B2 and subsequent B1, consecutive lock failures and timeouts, matched and card-only visitor tickets, duplicate and conflicting events, storage failure, Entrance Pricing, scanner and actor rejection, restart around gate authorization, terminal failures, archive rollback, and retained capture references.

| Check | Evidence |
|---|---|
| Backend restore | `dotnet restore NPark.sln` succeeded. |
| Backend build | `dotnet build NPark.sln --no-restore --nologo -clp:ErrorsOnly` succeeded with 17 warnings and zero errors. |
| Angular install and build | `npm ci` and `npm run build` succeeded. |
| Angular compatibility tests | ChromeHeadless, 2 passed, 0 failed, 0 skipped. |
| Angular full suite | 17 passed, 29 failed, 0 skipped. The reviewed baseline returned 15 passed and the same 29 named failures. Existing failures include missing Angular test providers. |
| EF migrations | `migrations list --no-connect` includes the new migration. `migrations has-pending-model-changes --no-build` reported no model changes. |
| SQL migration script | Generated and inspected the idempotent script. Applied it twice on an isolated database with `sqlcmd -b -I` and inspected all three tables and their indexes. `-I` enables quoted identifiers for the existing filtered indexes. |

| Final backend group | Executed | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|
| Entrance and parking event protocol | 18 | 18 | 0 | 0 |
| Gate dispatch, timeouts, and restart | 6 | 6 | 0 | 0 |
| Pricing, payment, and settlement regression | 7 | 7 | 0 | 0 |
| Startup, legacy migration, and gate protocol | 34 | 34 | 0 | 0 |
| Total | 65 | 65 | 0 | 0 |

The final backend run used `dotnet test NPark.Tests --no-build --artifacts-path <isolated-build> --filter <group> --logger "console;verbosity=normal"` across four disjoint filters covering all cases listed by `dotnet test --list-tests`. Set `NPARK_TEST_SQL` to an isolated SQL Server connection and `NPARK_TEST_MQTT_PORT` to a loopback broker. Each concurrent group that exercised MQTT used a separate listener. Earlier whole-solution runs exposed startup tests that assumed a single pending migration and a timing-dependent settlement fixture. The updated startup tests validate the full pending list, and the settlement fixture controls elapsed parking time independently of host scheduling.

The Angular test runner was invoked as `node ./node_modules/@angular/cli/bin/ng.js test --watch=false --browsers=ChromeHeadless`. The focused run added `--include=src/app/Domain/Gate/gate-operation.models.spec.ts`. The migration commands use `--project NPark.Infrastructure --startup-project NPark.Api --context NParkDBContext`.

Physical relay behavior, IIS rollout, live broker ACLs, and browser-to-physical-gate acceptance remain unverified. Firmware local actuation and offline event loss must be evaluated before rollout. The backend and simulator cases use isolated resources.
