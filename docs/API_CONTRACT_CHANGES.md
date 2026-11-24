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

Text and vehicle validations return `isValidForExit`, `reasonCode`, and an `eligibility` breakdown when an active ticket matches. `ScanTicket` provides the same assessment on its existing result. Important codes include `Eligible`, `TicketNotFound`, `TicketArchived`, `TicketAlreadyExited`, `AmbiguousTicket`, `InvalidExitGate`, `EntryNotConfirmed`, `BasePaymentRequired`, `SanctionPaymentRequired`, `OvernightPenaltyRequired`, and `GateOperationPending`. An unresolved operation is discoverable via the response or operation listing. Authorization at the selected gate rechecks eligibility immediately before reservation.

`CalculateQrCodeTicketSalary` and `CalculateSalaryForSubscriberCommand` retain the original response properties. `totalSalary` is now the base fee. `basePrice` and `exceedPrice` describe assessed amounts. `totalPrice` adds them. `paidAmount` and `outstandingAmount` describe the confirmed balance. `quotedSanctions` is the catalog total for the selected IDs. `requiredOvernightAmount` stays zero for compatibility because there is no predetermined quote. `requiresOvernightPenalty` indicates that a new payment is due. `CollectCashier` optionally takes `expectedBasePrice` to check the amount the operator confirms.

### Confirm a sanction payment

On `OvernightPenaltyRequired`, get the available sanctions from `GET /api/Sanction/GetAll`. Select any existing sanction or combination whose total is positive. Use the same selection and amount for the cashier's confirmation.

Send the confirmed amount, the ticket's observed validity date, and one stable key for this payment action.

```http
POST /api/TicketExceedTransaction/Create
```

```json
{
  "ticketId": "a791f0ea-5823-4f94-9d35-97c19b08c964",
  "sanctionIds": ["d027285e-7301-473a-9586-e0ca6ee75e33"],
  "expectedAmount": 50,
  "expectedValidExitDate": "2026-11-23",
  "idempotencyKey": "payment-action-001"
}
```

A successful first renewal returns these fields.

```json
{
  "ticketExceedTransactionId": "182c0b45-5c60-4b61-8ba7-847be44b048e",
  "ticketId": "a791f0ea-5823-4f94-9d35-97c19b08c964",
  "exceedAmount": 50,
  "exceedPrice": 50,
  "ticketPrice": 20,
  "totalPriceAfterExceed": 70,
  "paidAmount": 70,
  "outstandingAmount": 0,
  "validExitDate": "2026-11-24",
  "paymentStatus": "Confirmed"
}
```

- `expectedAmount` is required for every new payment. The server totals the current catalog amounts and returns HTTP 409 with `Payment.AmountChanged` if confirmation no longer matches. Refresh the quote and obtain fresh confirmation.
- `expectedValidExitDate` records the date shown to the cashier. A stale date is rejected with HTTP 409 and `Payment.ValidityChanged`. When a renewal has already been recorded today, a different action must include refreshed validity to be accepted as an ordinary payment. A replay returns the original transaction even if validity has since changed.
- `idempotencyKey` is required for overdue renewal. Reuse it after lost responses, including after midnight. A new charge requires a new key and cashier confirmation. Conflicting selections or confirmed amounts for the same key return HTTP 409. Legacy keyless ordinary payment requests use a date-independent selection identity.
- The response returns the same QR and the original amount for a replay. Revalidate eligibility before calling `ExitTicket`. Supervisor settlement remains separate.
