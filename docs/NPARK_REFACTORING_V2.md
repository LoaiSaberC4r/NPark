# NPark financial integrity V2

The backend now uses a shared eligibility decision for ticket exits. A database reservation protects both the ticket and the controller. Gate dispatch and ACK processing run outside the authorization transaction. A matching successful ACK commits independently. Exit completion copies the ticket and attachments to history, then removes the active ticket and completes the operation in one transaction.

Transaction-aware commands participate explicitly in the MediatR pipeline. Failure results and exceptions roll back changes. Nested failures prevent the outer commit. A transaction receipt in the same SQL transaction lets a fresh context verify uncertain commit outcomes. Side-effecting commands do not undergo general retries.

## Rollout

1. Review the [database migration](DATABASE_MIGRATION.md) and preflight a copy of production with representative data. Verify the business time zone and that the existing sanction catalog contains valid positive amounts.
2. Verify controller firmware and broker ACLs with the [hardware contract](MQTT_GATE_PROTOCOL.md). Keep retransmission disabled until firmware deduplication is demonstrated.
3. Implement the [Angular handoff](FRONTEND_INTEGRATION.md) and deploy it with the compatible backend and firmware in a coordinated window.
4. Verify ticket issuance, entry confirmation, same-day and overnight payment, exit timeout, late ACK, archive recovery, cashier settlement, and both supervisor reports. Reconcile unknown operations using recorded evidence when required.

See the [API contracts](API_CONTRACT_CHANGES.md), [financial settlement rules](FINANCIAL_SETTLEMENT.md), and [test results](TEST_RESULTS.md).
