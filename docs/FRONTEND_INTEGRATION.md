# Angular integration

Reference checkout reviewed locally at `1bc959325350a05054a80c4924e26e8d6a277009`. The Angular project sits outside the backend workspace. This handoff is required before release.

## Shared contracts

- In `src/app/Domain/tickets/tickets.model.ts` add `ExitEligibility` and the salary breakdown fields described in [API contracts](API_CONTRACT_CHANGES.md). Preserve the existing QR fields and transport.
- In `src/app/Services/Gate/gate-services.ts` replace the controller-only gate request with a typed body. Model `commandId` as a string and keep `operationId`, `status`, `ticketExited`, `archived`, `retryAllowed`, and `requiresReconciliation`.
- In `src/app/Services/Tickets/ticket-core.service.ts` and `tickets-services.ts` return the gate operation from `addExitTicket`. Use Angular's `observe: 'response'` where the UI needs to distinguish HTTP 202 from HTTP 200. Handle the response body on both status codes.
- Extend `src/app/Services/OverView/dashboard-hub.service.ts` and `src/app/Domain/OverView/dashboard-notifications.model.ts` to subscribe to `GateOperationStatusChanged`. Build the connection with the current gate JWT via `accessTokenFactory`. Reconnect on gate selection changes and refresh all pending operations after reconnecting. Use the HTTP status endpoint as the source of truth if events arrive out of order.
- In `src/app/Services/Gate/lpr-signalr.service.ts`, carry `operationId` and `operationStatus` through `mqttCaptureDetected`. A successful QR capture can carry a pending operation. Render it using the operation state.

## Entry

- In `src/app/Feature/Entry-Component/ticket-component.ts`, save the `ticketId` from both normal and subscriber `AddTicket` responses before the existing printing or gate flow. Print the existing QR payload.
- For the ticket gate action, send `{ controllerId, ticketId, operationType: 'Entry' }`. Show issued and entry pending while waiting. Show entry confirmed when status is `Completed`. If the operation fails or times out, retain the original ticket and payment. On refresh, recover the operation by its ticket ID and controller.
- Use `{ controllerId, operationType: 'Manual' }` only for the independent operator manual-open control.

## Exit

- In `src/app/Feature/exitgate-component/exitgate-component.ts`, route LPR, QR, card, and selected-ticket actions through unified eligibility and `addExitTicket(ticketId)`. Remove the follow-up `openGateForValidatedExit` and `openGateForSelectedTicket` calls from ticket workflows. The current `allowExitWithoutCollect` paths must follow the same backend payment rules.
- Replace `createMembershipExitTicketAndOpenGate` with a lookup of the subscriber's existing active ticket by card or vehicle. Display missing-ticket and ambiguous-match outcomes when appropriate. Ticket issuance belongs to the entry gate.
- Remove optimistic exit rows from the validation, confirmation, and MQTT paths. Add or refresh an exit row only when the operation reports `Completed` with `archived: true` or after the final `TicketExited` event.
- Persist enough local selection state to recover with `GET /api/Gate/operations?ticketId=…&controllerId=…`. Keep the ticket action disabled while the operation is unresolved. When retry is available, use the existing operation's retry endpoint. Surface unknown execution for authorized supervisor reconciliation.

## Payments and reports

- In `src/app/Services/TicketExceedTransaction/ticket-exceed-transaction.service.ts` and `src/app/Domain/TicketExceedTransaction/ticket-exceed-transaction.models.ts`, treat Create as confirmation that the cashier received the sanction payment. Generate and retain one `idempotencyKey` per receipt. Use the independent `ticketExceedTransactionId` as the row key. Revalidate eligibility after confirmation.
- In the exit component, display `basePrice`, `exceedPrice`, `paidAmount`, and `outstandingAmount` separately. Show `quotedSanctions` and `requiredOvernightAmount` as prospective charges. Pass `expectedBasePrice` with CollectCashier when the operator confirms the amount.
- In `src/app/Feature/supervisor-collections-report/` and `src/app/Services/Tickets/ticket-supervisor.service.ts`, use `sourceRecordId` and `financialItemType` for history. Display the actual responsible cashier when present and “Unknown” for null attribution. Show the global pending sanction summary independently from supervisor totals.
- In the dashboard models and views, surface pending entry, unresolved exit, and acknowledged exits awaiting archival independently from occupancy and historical completed exits.
