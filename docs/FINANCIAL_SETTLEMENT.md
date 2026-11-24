# Financial settlement

| Amount | Meaning |
|---|---|
| Base price | Parking fee assessed by entry or exit pricing |
| Exceed price | Accumulated assessed sanctions |
| Total price | Base price plus accumulated sanctions |
| Paid amount | Confirmed base payment plus confirmed sanctions |
| Outstanding amount | Assessed total less confirmed payment |

Entry Pricing records the cashier at issuance. Exit Pricing records the cashier and gate at CollectCashier. A paid base fee remains fixed. Subscribers and other valid zero-fee cases satisfy the base requirement without generating a cash receipt. Salary quotations show prospective sanctions separately from assessed balances. Sanction Create confirms customer payment, captures the applied catalog amounts, and assigns an independent transaction ID. The backend verifies the cashier's confirmed amount against the selected catalog total.

When validity expires, one newly confirmed positive payment for any existing catalog selection authorizes exit through the current business date. The transaction records one coverage row for that date, regardless of the number of missed days. The same catalog item may be selected on a later overdue date with a new payment key. A replay preserves the original charge, and a same-day ordinary payment leaves validity unchanged. Business dates come from `ParkingBusiness:TimeZoneId`. An empty or unusable catalog leaves the overnight requirement in effect.

The existing settlement header, lines, and items are retained. Base payments use the ticket ID across active and archived tables. Sanctions use their independent transaction IDs. These identities are globally unique across batches. Daily settlements use business-date payment windows and create the same history as supervisor batches. A history item records the individual amount and the responsible cashier when known. Historical unknown cashiers remain null for new settlements of legacy base payments. A supervisor settlement affects administrative collection fields and does not authorize vehicle exit.

The current-state report shows collected base and exceed payments for the requested supervisor and identifies its separate global pending exceed summary. The historical endpoint reads immutable collection snapshots by collection timestamp. Existing historical snapshots retain their recorded amounts and attribution.
