# Financial settlement

| Amount | Meaning |
|---|---|
| Base price | Parking fee assessed by entry or exit pricing |
| Exceed price | Accumulated assessed sanctions |
| Total price | Base price plus accumulated sanctions |
| Paid amount | Confirmed base payment plus confirmed sanctions |
| Outstanding amount | Assessed total less confirmed payment |

Entry Pricing records the cashier at issuance. Exit Pricing records the cashier and gate at CollectCashier. A paid base fee remains fixed. Subscribers and other valid zero-fee cases satisfy the base requirement without generating a cash receipt. Salary quotations show prospective sanctions separately from assessed balances. Sanction Create confirms customer payment, captures unit amounts and units, and assigns its own transaction ID. Only payment against the configured overnight sanction can extend the valid exit date.

With `PerAuthorizationDate`, one configured penalty authorizes exit on the current overdue business date. With `PerOverdueBusinessDay`, the payment covers each business date after the previous authorization through the current date. A unique key protects coverage for each ticket and date. Business dates come from `ParkingBusiness:TimeZoneId`. Set `OvernightParking:OvernightSanctionId` to the approved sanction with a positive amount. Missing configuration blocks overdue exit and confirmation.

The existing settlement header, lines, and items are retained. Base payments use the ticket ID across active and archived tables. Sanctions use their independent transaction IDs. These identities are globally unique across batches. Daily settlements use business-date payment windows and create the same history as supervisor batches. A history item records the individual amount and the responsible cashier when known. Historical unknown cashiers remain null for new settlements of legacy base payments. A supervisor settlement affects administrative collection fields and does not authorize vehicle exit.

The current-state report shows collected base and exceed payments for the requested supervisor and identifies its separate global pending exceed summary. The historical endpoint reads immutable collection snapshots by collection timestamp. Existing historical snapshots retain their recorded amounts and attribution.
