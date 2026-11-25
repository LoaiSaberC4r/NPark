# Acknowledged gate protocol

Existing QR captures continue on `publish/network/{controllerId}`. Gate commands publish binary bytes on `sub/network/{controllerId}`. Controllers respond on `ack/network/{controllerId}`. Use canonical decimal controller IDs 1 through 65535. Multi-byte integers are unsigned big endian on the wire.

| Packet | Offsets inclusive | Meaning |
|---|---|---|
| Open, 16 bytes | 0, 1–2, 3–10, 11–14, 15 | `7B`, controller ID, command ID, `AE BE C1 B1`, `7D` |
| ACK, 13 bytes | 0, 1–2, 3–10, 11, 12 | `7B`, controller ID, command ID, execution status, `7D` |

Command example for controller 2 and command 43 is `7B 00 02 00 00 00 00 00 00 00 2B AE BE C1 B1 7D`. Successful ACK is `7B 00 02 00 00 00 00 00 00 00 2B 00 7D`.

| ACK status | Result |
|---|---|
| `00` | Success after relay execution |
| `01` | Execution failed |
| `02` | Gate busy |
| `03` | Command rejected |

SQL Server allocates positive BIGINT command IDs from a noncycling sequence. MQTT broker acknowledgment confirms delivery to the broker. NPark accepts operational success when a matching hardware ACK with code `00` is durably recorded. The approved convention does not require a physical sensor confirmation.

## Broker configuration

Development and Testing use `192.168.1.40:1883` with MQTT 3.1.1, a 20-second keep-alive, a 3-second reconnect interval, and TLS disabled. The base settings require the deployment to supply a broker host. Configure `Mqtt__Host` and any required port, TLS, or authentication overrides through the deployment environment.

Set `Mqtt__ClientId` to a distinct, stable value for every API process that can run concurrently on the same broker. The ACK subscriber uses the `-gate-ack` suffix and keeps its session across restarts. The publisher uses the base ID and the QR subscriber adds `-subscriber`. If an IIS application pool shares one configured ID, use a single worker and disable overlapping recycling for that pool. Allow the configured client identifiers and topic filters in the broker ACL.

## Firmware and broker verification

- Check the command and ACK samples against firmware with a test controller. Verify maximum controller ID, fixed packet sizes, big endian fields, and correlations at the correct offsets. ACK only after the relay pulse. Use QoS 1 by default and disable retain. Verify that normal broker redelivery of an identical command cannot pulse the relay twice.
- Give each controller access to its own command, ACK, and capture topics with broker ACLs and TLS where deployed. Restrict ACK publication to the controller identity. The backend subscribes to ACKs separately from QR captures with a persistent MQTT session. Give each running backend instance a unique MQTT client ID.
- Publish ACKs with retain disabled. The backend rejects retained deliveries. MQTT 3.1.1 clears the retain flag when forwarding a live publication to a subscription, so the controller or broker must enforce the publication rule.
- MQTT QoS 1 can redeliver the same frame at the transport layer. Every new application-level gate attempt has a new command ID. Legacy application retransmission settings are ignored.

## Recovery

| State | Controller reserved | Ticket reserved |
|---|---|---|
| `Pending` | Yes, bounded | Yes, bounded |
| `Published` | Yes, bounded | Yes, bounded |
| `Acknowledged` | Released in the ACK transaction | Until completion commits |
| `Completed`, `Failed`, `TimedOut`, `Unknown` | Released | Released |

Manual operations have no ticket reservation. A valid hardware error ACK commits `Failed`. An unclaimed command can be dispatched after a restart if the publisher and ACK subscription are ready before `GateOperation:DispatchWaitTimeoutSeconds` (default 10 seconds from creation). Unclaimed expiry is `TimedOut` with `GateDispatchTimeout`. A committed dispatch claim is used once. The publisher has `GateOperation:AckTimeoutSeconds` (default 3 seconds) from the claim to settle the attempt. A thrown, canceled, or abandoned publish becomes `Unknown` with `GateDeliveryUnknown` after the attempt settles or its SQL dispatch lock is gone. A published operation has the configured ACK interval from `PublishedAtUtc` and becomes `TimedOut` with `GateAckTimeout` on expiry. The worker checks deadlines at `GateOperation:RecoveryIntervalSeconds` (default 2 seconds).

An ACK must match the controller and command and be processed before the applicable deadline. The exact deadline belongs to expiry. Late ACKs are logged and leave terminal execution outcomes unchanged. A successful ACK releases the controller before ticket confirmation or archival. Recovery can repeat the database completion transaction after a failure while preserving the ticket reservation and the original ACK. The timeout and unknown outcomes can follow actual relay execution. A newly authorized request may move the relay again if the earlier command did execute.

An active ticket operation stays bound to its original controller after a gate reassignment. The exit endpoint returns HTTP 409 with `GateOperationControllerChanged` and the existing operation ID. A fresh request after unsuccessful termination uses the current mapping. The ACK subscriber must receive a QoS 1 or 2 subscription grant before recovery can dispatch. A disconnect clears readiness. A failed ACK database write leaves the MQTT delivery unconfirmed so the persistent session can redeliver it within the applicable deadline.

## Deployment

Stop all pre-release API and gate-worker instances that share the database before starting this version. Disable overlapping IIS recycling during the rollout and enable traffic after the updated instances start. Startup honors `Database:ApplyMigrationsOnStartup`. After the schema check and before starting any MQTT worker, an idempotent SQL transaction clears both reservation fields for terminal operations and clears the controller reservation for successful `Acknowledged` records. It preserves history and ticket reservations awaiting completion. This release uses the existing schema, filtered unique indexes, row versions, and command ID sequence. Apply the same coordinated stop procedure if rolling back to a release with the previous reservation rules.

Before rollout, verify the simulator and compatible physical firmware, then test entry, same-day and overdue exit, timeout and late ACK, restart, database recovery, and manual open. The simulator covers the protocol paths listed in [test results](TEST_RESULTS.md).
