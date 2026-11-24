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
