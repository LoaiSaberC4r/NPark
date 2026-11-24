SET NOCOUNT ON;

-- Invalid sources and unverifiable legacy exceed payment references.
SELECT item.Id AS CollectionItemId, item.TicketCollectionTransactionId,
       item.Source, item.TicketIdSnapshot, item.AmountCollected
FROM dbo.TicketCollectionTransactionTickets AS item
LEFT JOIN dbo.TicketExceedTransactions AS payment
    ON payment.Id = item.TicketIdSnapshot AND payment.TicketIdSnapshot = item.TicketIdSnapshot
WHERE item.Source NOT IN (1, 2, 3)
   OR item.TicketIdSnapshot = '00000000-0000-0000-0000-000000000000'
   OR (item.Source = 3 AND payment.Id IS NULL);

-- Duplicate settlement identities across active, archive, and separate collection batches.
WITH SettlementHistory AS (
    SELECT Id, TicketCollectionTransactionId, TicketIdSnapshot, Source, AmountCollected,
           CASE WHEN Source = 3 THEN 2 ELSE 1 END AS FinancialItemType,
           COUNT_BIG(*) OVER (
               PARTITION BY CASE WHEN Source = 3 THEN 2 ELSE 1 END, TicketIdSnapshot
           ) AS DuplicateCount
    FROM dbo.TicketCollectionTransactionTickets
)
SELECT Id AS CollectionItemId, TicketCollectionTransactionId, TicketIdSnapshot, Source,
       FinancialItemType, AmountCollected, DuplicateCount
FROM SettlementHistory
WHERE DuplicateCount > 1
ORDER BY FinancialItemType, TicketIdSnapshot, TicketCollectionTransactionId;

-- Active/archive conflicts are evidence for separate operational review.
SELECT t.Id, t.StartDate, t.EndDate AS ActiveEndDate, a.EndDate AS ArchivedEndDate,
       t.IsCollected AS ActiveCollected, a.IsCollected AS ArchivedCollected
FROM dbo.Tickets AS t
JOIN dbo.TicketArchive AS a ON a.Id = t.Id;
