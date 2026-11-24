# API contracts

## Gate operations

`POST /api/Ticket/ExitTicket` takes the existing `{ "ticketId": "…" }` body. `POST /api/Gate/open` takes `{ "controllerId": "2", "operationType": "Entry", "ticketId": "…" }` or `{ "controllerId": "2", "operationType": "Manual" }`. Ticket-associated exit uses the ticket route. Controller IDs are unsigned 16-bit IDs from the gate's registered control value.

Both routes return an operation. HTTP 202 contains a `Location` header for the pending operation. HTTP 200 carries a terminal operation whose `status` identifies success or failure. Existing validation and authorization errors use the normal problem response. This is a pending exit response.

```json
{
  "operationId": "182c0b45-5c60-4b61-8ba7-847be44b048e",
  "ticketId": "d027285e-7301-473a-9586-e0ca6ee75e33",
  "commandId": "43",
  "controllerId": 2,
  "operationType": "Exit",
  "status": "Published",
  "isAcknowledged": false,
  "ticketExited": false,
  "archived": false
}
```

- `GET /api/Gate/operations/{operationId}` reads current state. `GET /api/Gate/operations` lists up to 100 recent matching operations. Filters include `ticketId`, `controllerId`, and `status`.
- `POST /api/Gate/operations/{operationId}/retry` reattempts an acknowledged operation's database completion or resends the original command when deduplication is confirmed and retries remain. An unpermitted retry returns HTTP 409.
- `POST /api/Gate/operations/{operationId}/reconcile-non-execution` requires a Supervisor at the selected gate and `{ "evidence": "…" }` with a verified reason. Resolution persists the actor, evidence, and time. A hardware ACK remains the authority for completion.
- Operation reads and actions require the authenticated gate. Gate-scoped clients receive `GateOperationStatusChanged` on `/hubs/notifications`. Treat events as refresh signals and handle duplicates. `TicketExited` follows archive commit. The existing QR capture notification can contain an operation ID while its terminal booleans are false.
- Clients must preserve `commandId` as a decimal string. `topic` and `payload` describe the binary command as its destination and hex text. ACK and operation timestamps are UTC.

## Eligibility and financial fields

Text and vehicle validations return `isValidForExit`, `reasonCode`, and an `eligibility` breakdown when an active ticket matches. `ScanTicket` provides the same assessment on its existing result. Important codes include `Eligible`, `TicketNotFound`, `TicketArchived`, `TicketAlreadyExited`, `AmbiguousTicket`, `InvalidExitGate`, `EntryNotConfirmed`, `BasePaymentRequired`, `SanctionPaymentRequired`, `OvernightPenaltyConfigurationMissing`, `OvernightPenaltyRequired`, and `GateOperationPending`. An unresolved operation is discoverable via the response or operation listing. Authorization at the selected gate rechecks eligibility immediately before reservation.

`CalculateQrCodeTicketSalary` and `CalculateSalaryForSubscriberCommand` retain the original response properties. `totalSalary` is now the base fee. `basePrice` and `exceedPrice` describe assessed amounts. `totalPrice` adds them. `paidAmount` and `outstandingAmount` describe the confirmed balance. `quotedSanctions` and `requiredOvernightAmount` are prospective amounts. `CollectCashier` optionally takes `expectedBasePrice` to check the amount the operator confirms.

`TicketExceedTransaction/Create` confirms receipt of the selected sanction payment. Send the same `idempotencyKey` for retries of one cashier action. The configured overnight sanction ID must appear in the selection when confirming an overnight charge. The response identifies the independent payment, original ticket and QR, amount, updated accumulated balance, and valid exit date. A conflicting selection or coverage produces an error. Administrative settlement has separate endpoints and returns the persisted transaction ID.
