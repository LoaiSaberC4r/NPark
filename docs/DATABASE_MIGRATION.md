# SQL Server migration

## Startup configuration

`Database:ApplyMigrationsOnStartup` controls whether the API applies pending EF Core migrations at startup.

| Environment | Automatic startup migration |
|---|---|
| Default or missing setting | Disabled |
| Development | Enabled |
| Testing | Enabled |
| Production | Disabled |

Override the setting with the environment variable `Database__ApplyMigrationsOnStartup`. For IIS, place the variable under the ASP.NET Core Module's `<aspNetCore><environmentVariables>` section in `web.config`, then recycle the app to load the new configuration.

```xml
<environmentVariable name="Database__ApplyMigrationsOnStartup" value="true" />
```

Startup logs the names of pending migrations. When enabled, EF Core applies them and startup verifies that none remain before running seeding. When disabled, a pending migration stops startup with the setting name and manual migration guidance. A schema that is already up to date proceeds directly to seeding.

Migration and seeding run before the API and its hosted workers start, including with concurrent hosted-service startup enabled. A migration or connectivity error is logged with its original exception and stops startup before seeding. Cancellation interrupts database initialization and prevents workers from starting.

## Manual deployment

Production defaults to applying reviewed migrations separately. Review and back up the database, rehearse against representative data, and schedule a maintenance window. Drain incompatible API and worker instances before changing the schema. Use a single designated migrator for a multi-instance deployment. Run the preflight and upgrade below, then verify migration history, unique indexes, and report totals before starting the API instances.

## Financial integrity upgrade

Apply from the last committed migration, `20260505150257_AddingSuperVisorRecord`, using [preflight SQL](NPARK_FINANCIAL_INTEGRITY_V2_PREFLIGHT.sql) and the [idempotent upgrade](NPARK_FINANCIAL_INTEGRITY_V2.sql). Use SQLCMD with `-b` so an error stops the script. Run against a rehearsal database before rollout and review the database backup and migration history.

- Legacy `ValidExitDate` is backfilled from the calendar date of the original `StartDate`, which was stored as business-local time. Ticket IDs, QR values, original dates, and source relationships stay unchanged.
- Existing entries use `Legacy` confirmation status. Historical ACK and cash payment timestamps remain unknown. A valid legacy cashier-paid flag satisfies the base requirement. Historical exceed Create records retain their legacy paid interpretation. Overdue legacy records still need coverage under the configured policy.
- Base collection items backfill the original `TicketIdSnapshot` into `SourceRecordId`. Exceed items are backfilled only after the existing payment is verified to have the historical matching ID and ticket ID. The migration stops on unresolved references or duplicate financial identities and rolls back. The preflight identifies individual items and collection batches for investigation.
- Review duplicates with finance staff and compare receipts, supervisor batches, and historical sanction references. Preserve the evidence and agree on a reviewed data-repair script for the specific records before restarting migration. Review pre-existing active/archive overlaps and suspicious historical base values individually. Migration leaves historical amounts unchanged.
- The down migration refuses when it would lose acknowledged gate history or transaction metadata or when multiple exceed payments for one ticket cannot be represented in the previous schema. Use a reviewed backup and replay process or a forward repair once the new workflow has been used.
