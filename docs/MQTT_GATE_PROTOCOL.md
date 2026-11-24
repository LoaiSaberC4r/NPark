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

## Firmware and broker verification

- Check the command and ACK samples against firmware with a test controller. Verify maximum controller ID, fixed packet sizes, big endian fields, and correlations at the correct offsets. ACK only after the relay pulse. Use QoS 1 by default and disable retain. Verify that normal broker redelivery of an identical command cannot pulse the relay twice.
- Give each controller access to its own command, ACK, and capture topics with broker ACLs and TLS where deployed. Restrict ACK publication to the controller identity. The backend subscribes to ACKs separately from QR captures with a persistent MQTT session. Give each running backend instance a unique MQTT client ID.
- Before enabling retransmission, verify firmware repeats the original execution response for a duplicate command ID and avoids a second relay pulse. Confirm retention and restart behavior over the actual retry and recovery window. Until then, the default configuration keeps automatic and manual retransmission of uncertain operations disabled.

## Recovery

Pending commands whose dispatch has never been claimed can be sent after a restart. A claimed command whose delivery is uncertain keeps its reservations. A published command without an ACK becomes `TimedOut`. A broker or transport error can produce `Unknown`. Both accept late matching ACKs. A successful ACK commits before archive work starts, and recovery completes its database work using the original ACK timestamp and command. When a supervisor can independently establish non-execution, record the evidence through the reconciliation endpoint.

Before rollout, verify the simulator and compatible physical firmware, then test entry, same-day and overdue exit, timeout and late ACK, restart, database recovery, and manual open. The simulator covers the protocol paths listed in [test results](TEST_RESULTS.md).
