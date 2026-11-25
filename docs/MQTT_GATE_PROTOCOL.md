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

## MQTT v1.1.11 entrance integration

The backend persists accepted LPR detections with the raw plate, normalized plate, gate, controller snapshot, and UTC receipt time. The archive worker moves captures older than 72 hours in transactions of up to 1,000 rows, checking at startup and every 60 minutes by default. Archived captures keep their original identifiers and ticket references.

For the subscriber path, an entrance controller must be on the v1.1.11 Lock allowlist. The API resolves the gate from the registered `lprIp` in the request payload. Direct cameras, proxies, and integration services follow the same workflow regardless of the HTTP source IP. An eligible subscriber has a current membership and no active subscriber ticket. The backend sends B2 and waits within the existing operation deadline for a terminal result before sending the usual LPR notification. A 60-second default deduplication window applies to the same normalized plate and gate. The firmware holds the lock for up to 60 seconds and releases it on Open. SQL releases its controller reservation through the gate lifecycle. The frontend continues subscriber issuance and B1 opening.

| Frame | Topic | Example for controller 1 |
|---|---|---|
| Lock with command ID 101 | `sub/network/1` | `7B 00 01 00 00 00 00 00 00 00 65 AE BE C1 B2 7D` |
| Open with command ID 102 | `sub/network/1` | `7B 00 01 00 00 00 00 00 00 00 66 AE BE C1 B1 7D` |
| Successful Lock ACK | `ack/network/1` | `7B 00 01 00 00 00 00 00 00 00 65 00 7D` |
| Visitor pickup event | `parking/v1/event/1` | `7B 00 01 00 00 03 15 00 00 00 01 01 01 01 1D 6B 05 27 E0 04 7B 00 7D AB 7D` |
| Storage ACK | `parking/v1/ack/1` | `7B 00 01 00 00 03 15 00 00 00 01 00 7D` |

Commands and storage ACKs are raw binary, use QoS 1 with retain disabled, and use canonical decimal controller suffixes. Command dispatch still requires a usable subscription on `ack/network/#` or `ack/network/+`. Lock and Open have separate sequence-generated command IDs. ACK status 00 confirms the matching action. Statuses 01, 02, and 03 fail the matching operation. A B2 result does not affect a ticket.

| Schema 01 byte offset | Field |
|---|---|
| 0 | Start marker `7B` |
| 1–2 | Controller ID, unsigned big-endian |
| 3–10 | Eight-byte Event ID |
| 11–14 | Schema, lane, outcome, flags |
| 15–18 | Raw unsigned site wall-clock seconds |
| 19 | UID length `0` or `4` |
| 20 through 19 + UID length | UID bytes in reader order |
| 20 + UID length | End marker `7D` |

The decoder accepts exact lengths of 21 or 25 bytes for Schema 01. Schema 02 adds four expiry bytes, one card gate byte, and one grace byte ahead of the end marker. A validated Schema 02, exit event, or failed card read is stored for audit with no visitor action. The visitor pickup requires schema 01, lane 01, outcome 01, and four UID bytes. The 13-byte storage acknowledgment copies bytes 0–10, appends status `00`, and ends in `7D`.

The subscriber uses MQTT 3.1.1 with a distinct `<Mqtt.ClientId>-parking-events` client ID, a bounded intake queue, and a `parking/v1/event/+` subscription. Valid intake commits to SQL before any successful storage ACK. A byte-identical replay receives another storage ACK. A conflicting payload with the same controller and Event ID keeps the original row and receives no success ACK. The worker resumes committed events and records the original UTC receipt, raw event payload, flags, and optional Cairo wall-clock conversion. Full UID-bearing frames are available in restricted SQL audit records. Routine logs carry protocol and operational identifiers.

### Enablement and rollout

1. Disable overlapping IIS recycling and stop older instances sharing the database before applying the additive migration. Keep `Database:ApplyMigrationsOnStartup` in line with the site's migration procedure. Review the schema and existing reservation indexes before starting workers.
2. Retain the existing broker configuration. Development and Testing point to `192.168.1.40:1883` by default. Override the broker to an isolated listener when testing. Use distinct MQTT client IDs for multiple workers. Enforce broker topic ACLs and network isolation for firmware using plain unauthenticated MQTT. Keep access to the legacy LPR endpoint limited to the trusted camera network and approved integrations.
3. Enable `SubscriberButtonLock` and list verified controllers. Enable `MqttParkingEvents` for intake. To permit visitor issuance, enable `AutomaticVisitorEntry`, set its controller allowlist, and provision a dedicated existing database user for `SystemUserId`. Use Exit Pricing and gates that do not require scanner photographs. Use a configuration test environment with features enabled explicitly.
4. Verify the physical firmware before enabling either movement feature on site. Ordinary card pickup on v1.1.11 may already pulse the relay. Bit 2, `RelayPulseIssued`, is recorded, and the backend still sends one correlated B1 command for successful automatic issuance. The resulting movement may repeat, or the controller may return Busy. Under Entrance Pricing, the backend stores the event and declines issuance and B1. A local pulse may already have happened. A command ACK confirms the controller's relay pulse and provides no physical barrier-position feedback.
5. To roll back, turn off both movement features, drain or inspect outstanding operations, and coordinate IIS worker shutdown before restarting. Keep the additive schema to retain event and capture history. The EF Down migration removes the new history tables.

Firmware parking events begin at QoS 0, even with a QoS 1 subscription. An application storage ACK covers an SQL commit. Events that never reach the API during an outage may be lost because the firmware has no durable offline replay. Retained deliveries identifiable by the MQTT flag are ignored.

Set `SubscriberButtonLock__SupportedControllerIds__0` and `AutomaticVisitorEntry__SupportedControllerIds__0` for the first allowed controller in each feature. Set `AutomaticVisitorEntry__SystemUserId` to the provisioned account's database ID. The corresponding `__Enabled` environment keys enable each feature, including `MqttParkingEvents__Enabled` for intake. Leave both movement features disabled during migration and initial broker verification.
