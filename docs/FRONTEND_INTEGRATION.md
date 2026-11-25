# Angular integration

Reference checkout reviewed locally at `1bc959325350a05054a80c4924e26e8d6a277009`. The Angular project sits outside the backend workspace. This handoff is required before release.

## Shared contracts

- In `src/app/Domain/tickets/tickets.model.ts` add `ExitEligibility` and the salary breakdown fields described in [API contracts](API_CONTRACT_CHANGES.md). Preserve the existing QR fields and transport.
- In `src/app/Services/Gate/gate-services.ts` replace the controller-only gate request with a typed body. Model `commandId` as a string and keep `operationId`, `status`, `ticketExited`, `archived`, `retryAllowed`, `requiresReconciliation`, `reasonCode`, and `reasonMessage`.
- In `src/app/Services/Tickets/ticket-core.service.ts` and `tickets-services.ts` return the gate operation from `addExitTicket`. Use Angular's `observe: 'response'` where the UI needs to distinguish HTTP 202 from HTTP 200. Handle the response body on both status codes.
- Extend `src/app/Services/OverView/dashboard-hub.service.ts` and `src/app/Domain/OverView/dashboard-notifications.model.ts` to subscribe to `GateOperationStatusChanged`. Build the connection with the current gate JWT via `accessTokenFactory`. Reconnect on gate selection changes and refresh all pending operations after reconnecting. Use the HTTP status endpoint as the source of truth if events arrive out of order.
- In `src/app/Services/Gate/lpr-signalr.service.ts`, carry `operationId` and `operationStatus` through `mqttCaptureDetected`. A successful QR capture can carry a pending operation. Render it using the operation state.

## Monitoring and new attempts

1. Generate a `requestId` UUID when the operator explicitly requests entry, exit, or manual opening. Keep the UUID and body across transport retries of that action. Use a new UUID for a subsequent explicit action.
2. Follow the operation ID returned by the backend. Poll or refresh it with `GET /api/Gate/operations/{operationId}`. Treat SignalR events and reconnects as prompts to refresh. Disable the same ticket action while it is `Pending`, `Published`, or `Acknowledged`.
3. Show success when the status is `Completed`. On `Failed`, `TimedOut`, or `Unknown`, stop polling the attempt as active, show the reason, and re-enable the normal action when eligible. For an exit ACK timeout, Arabic copy can be “لم يتم تأكيد فتح البوابة. لم يتم تسجيل خروج التذكرة. يمكنك المحاولة مرة أخرى.” An uncertain delivery can still have moved the physical relay.
4. For a fresh exit attempt, refresh the existing ticket's eligibility and submit `ExitTicket` again only on operator action. Reuse the existing ticket for entry and keep its payment and QR code. Use the normal manual-open control for a new manual attempt. The retry route is for database completion of `Acknowledged` operations. Supervisors may add an audit annotation independently of the operator flow.

## Entry

- In `src/app/Feature/Entry-Component/ticket-component.ts`, save the `ticketId` from both normal and subscriber `AddTicket` responses before the existing printing or gate flow. Print the existing QR payload.
- For the ticket gate action, send `{ controllerId, ticketId, operationType: 'Entry' }`. Show issued and entry pending while waiting. Show entry confirmed when status is `Completed`. If the operation fails or times out, retain the original ticket and payment. On refresh, recover the operation by its ticket ID and controller.
- Use `{ controllerId, operationType: 'Manual' }` only for the independent operator manual-open control.

## Exit

- In `src/app/Feature/exitgate-component/exitgate-component.ts`, route LPR, QR, card, and selected-ticket actions through unified eligibility and `addExitTicket(ticketId)`. Remove the follow-up `openGateForValidatedExit` and `openGateForSelectedTicket` calls from ticket workflows. The current `allowExitWithoutCollect` paths must follow the same backend payment rules.
- Replace `createMembershipExitTicketAndOpenGate` with a lookup of the subscriber's existing active ticket by card or vehicle. Display missing-ticket and ambiguous-match outcomes when appropriate. Ticket issuance belongs to the entry gate.
- Remove optimistic exit rows from the validation, confirmation, and MQTT paths. Add or refresh an exit row only when the operation reports `Completed` with `archived: true` or after the final `TicketExited` event.
- Persist enough local selection state to recover with `GET /api/Gate/operations?ticketId=…&controllerId=…`. Refresh the current gate mapping and financial eligibility before offering another exit attempt.

## Payments and reports

- In `src/app/Services/TicketExceedTransaction/ticket-exceed-transaction.service.ts` and `src/app/Domain/TicketExceedTransaction/ticket-exceed-transaction.models.ts`, treat Create as confirmation that the cashier received the sanction payment. When validation returns `OvernightPenaltyRequired`, load the existing catalog with `GET /api/Sanction/GetAll` and let the cashier select any sanctions with a positive total. Submit the confirmed total as `expectedAmount`, the last validated `validExitDate` as `expectedValidExitDate`, and a stable `idempotencyKey` for the action. Keep the key across retries and refreshes, including after midnight. Generate a new key only for a newly confirmed payment. Use the independent `ticketExceedTransactionId` as the row key. Revalidate eligibility after confirmation, then call `ExitTicket` only when eligible.
- In the exit component, display `basePrice`, `exceedPrice`, `paidAmount`, and `outstandingAmount` separately. Display the selected catalog total as the prospective charge. Use `requiresOvernightPenalty` to prompt for selection because `requiredOvernightAmount` is always zero. On a price or validity conflict, refresh the quote or ticket and request fresh confirmation. Pass `expectedBasePrice` with CollectCashier when the operator confirms the amount.
- In `src/app/Feature/supervisor-collections-report/` and `src/app/Services/Tickets/ticket-supervisor.service.ts`, use `sourceRecordId` and `financialItemType` for history. Display the actual responsible cashier when present and “Unknown” for null attribution. Show the global pending sanction summary independently from supervisor totals.
- In the dashboard models and views, surface pending entry, unresolved exit, and acknowledged exits awaiting archival independently from occupancy and historical completed exits.

## MQTT v1.1.11 entrance compatibility

The Angular change starts from `3e78f72e6200a5a24ac3245a08d2dd6b4c65f2cc`. Gate operation normalization recognizes `LockCardButton` and numeric value 4, preserves Manual as 3, and treats Failed, TimedOut, and Unknown as terminal. Lock records are excluded from entry completion and do not enter the cashier's Open flow.

The existing LPR membership check, subscriber AddTicket, manual visitor AddTicket, printing, and operator Open flow continue at their existing entry points. The automatic visitor flow uses the existing authenticated dashboard hub. The entry screen refreshes its same-gate ticket list on automatic `TicketAdded`, completed automatic Entry status, and reconnect. These notifications do not invoke AddTicket, Open, printing, or the cashier's interactive pending-entry state. The backend still requires the correlated Open ACK before it confirms entry.

Automatic visitor tickets may have a null vehicle number. Use the existing ticket ID for the list refresh. Treat the initial issuance and later entry confirmation as separate events.
