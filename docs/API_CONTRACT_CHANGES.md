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
- Both authorization bodies accept optional `requestId`, a non-empty UUID generated once per deliberate gate action. Send the same UUID, body, and authenticated operator/gate when replaying an HTTP request with an uncertain response. Once accepted, its `operationId` is that UUID and replays return the same operation, including after termination or a gate remap. Reusing the UUID for different request details returns HTTP 409 with `GateRequestIdConflict`. Requests without a UUID retain active-operation deduplication. Only a UUID replay can identify a previously accepted request after unsuccessful termination. A different UUID submitted while the gate or ticket is reserved may receive HTTP 409. Handle the conflict and require a new operator action before sending another request.
- `Pending` and `Published` reserve the controller and, for ticket operations, the ticket. `Acknowledged` means hardware success is durable and ticket completion may still be pending. It reserves only the ticket. `Completed` indicates success. `Failed`, `TimedOut`, and `Unknown` are unsuccessful terminal attempts with `retryAllowed`, `requiresReconciliation`, `ticketExited`, and `archived` all false.
- `POST /api/Gate/operations/{operationId}/retry` attempts database completion for `Acknowledged` only. Terminal operations return HTTP 409 with `GateFreshRequestRequired`. Start another authorized attempt through the normal endpoint. A completed entry or exit remains protected against repeat execution.
- `POST /api/Gate/operations/{operationId}/reconcile-non-execution` is an optional audit action for `TimedOut` and `Unknown`. It requires a Supervisor at the selected gate and `{ "evidence": "…" }` with a verified reason. It records the actor, evidence, and time on the original attempt while preserving its status and reason code.
- Operation reads and actions require the authenticated gate. Gate-scoped clients receive `GateOperationStatusChanged` on `/hubs/notifications`. Treat events as refresh signals and handle duplicates. `TicketExited` follows archive commit. The existing QR capture notification can contain an operation ID while its terminal booleans are false.
- Clients must preserve `commandId` as a decimal string. `topic` and `payload` describe the binary command as its destination and hex text. ACK and operation timestamps are UTC. Terminal failure responses include a stable `reasonCode` and a localized `reasonMessage`. Codes include `GateDispatchTimeout`, `GateAckTimeout`, `GateDeliveryUnknown`, `GateExecutionFailed`, `GateBusy`, and `GateRejected`.

## Eligibility and financial fields

Text and vehicle validations return `isValidForExit`, `reasonCode`, and an `eligibility` breakdown when an active ticket matches. `ScanTicket` provides the same assessment on its existing result. Important codes include `Eligible`, `TicketNotFound`, `TicketArchived`, `TicketAlreadyExited`, `AmbiguousTicket`, `InvalidExitGate`, `EntryNotConfirmed`, `BasePaymentRequired`, `SanctionPaymentRequired`, `OvernightPenaltyRequired`, and `GateOperationPending`. `existingOperationId` can identify an active reservation or an acknowledged operation completing the ticket. Historical unsuccessful attempts stay available through the operation listing. Authorization at the selected gate rechecks eligibility immediately before reservation.

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

## Automatic entrance processing

- `POST /api/lpr-signalr/vehicle-detected` keeps its existing input and response. The `lprVehicleDetected` event on `/hubs/lpr` still contains `gateId` and `vehicleNumber`, including for exit gates and non-subscribers. If LPR persistence fails, the API logs the degraded capture and still notifies when possible. With B2 enabled, eligible detections from the mapped camera wait for the bounded Lock outcome before notification.
- Existing endpoint authorization and request contracts remain in effect. The public `POST /api/Gate/open` still accepts the existing public operation choices. Gate operation responses and history can now include the string `LockCardButton` for a ticketless internal action. Persisted operation values are Entry 1, Exit 2, Manual 3, and LockCardButton 4. `commandId` remains a decimal string.
- Automatic visitor commands use existing Entry status and currently authorized gate history. Their responses and gate-scoped `GateOperationStatusChanged` events include `isAutomaticVisitorEntry: true`. An automatic ticket's gate-scoped `TicketAdded` has `isAutomatic: true`, the existing ticket identity, and its initial data. Manual issuance and its existing dashboard event still use their established contract.
- The backend issues a normal unpaid visitor ticket under Exit Pricing, with the hardware UID, actor, entrance gate, original receipt's Cairo business date, and the latest capture at or before receipt from that gate and controller. If the latest capture was already claimed, the ticket uses only the card. The optional `LprCapture:MaxMatchingAgeSeconds` is disabled by default. Infrequent camera reads can leave a stale latest plate. Independent cashier and hardware visits may create two tickets where existing rules permit it.
- Under Entrance Pricing or when required scanner data, a valid actor, or an eligible controller is unavailable, the event is stored and acknowledged and issuance is rejected. A storage ACK and ticket creation carry no entry confirmation. Only correlated B1 success completes a pending ticket. Terminal failures preserve the ticket and release the controller without automatic retransmission.
