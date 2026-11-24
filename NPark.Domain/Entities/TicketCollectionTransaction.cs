using BuildingBlock.Domain.EntitiesHelper;
using NPark.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Domain.Entities
{
    public sealed class TicketCollectionTransaction : Entity<Guid>
    {
        private readonly List<TicketCollectionTransactionLine> _lines = new();
        private readonly List<TicketCollectionTransactionTicket> _tickets = new();

        private TicketCollectionTransaction()
        {
        }

        public TicketCollectionTransactionType Type { get; private set; }

        public Guid GateId { get; private set; }

        public ParkingGate Gate { get; private set; } = null!;

        public Guid CollectedBy { get; private set; }

        public User Collector { get; private set; } = null!;

        public DateTime CollectedAt { get; private set; }

        public int TotalItemsCollected { get; private set; }

        public decimal TotalAmountCollected { get; private set; }

        public IReadOnlyCollection<TicketCollectionTransactionLine> Lines
            => _lines.AsReadOnly();

        public IReadOnlyCollection<TicketCollectionTransactionTicket> Tickets
            => _tickets.AsReadOnly();

        public static TicketCollectionTransaction Create(
            TicketCollectionTransactionType type,
            Guid gateId,
            Guid collectedBy,
            DateTime collectedAt)
        {
            if (gateId == Guid.Empty)
                throw new ArgumentException("Gate id is required.", nameof(gateId));

            if (collectedBy == Guid.Empty)
                throw new ArgumentException("Collector id is required.", nameof(collectedBy));

            return new TicketCollectionTransaction
            {
                Id = Guid.NewGuid(),
                Type = type,
                GateId = gateId,
                CollectedBy = collectedBy,
                CollectedAt = collectedAt,
                TotalItemsCollected = 0,
                TotalAmountCollected = 0
            };
        }

        public void AddTicket(TicketCollectionTransactionTicket ticket)
        {
            ArgumentNullException.ThrowIfNull(ticket);

            if (ticket.TicketCollectionTransactionId != Id)
                throw new InvalidOperationException("Ticket snapshot belongs to another collection transaction.");

            _tickets.Add(ticket);

            RecalculateTotals();
        }

        public void BuildLinesFromTickets()
        {
            _lines.Clear();

            if (_tickets.Count == 0)
                throw new InvalidOperationException("Cannot build collection lines without ticket snapshots.");

            var groups = _tickets
                .GroupBy(x => x.ResponsibleUserId)
                .Select(g => TicketCollectionTransactionLine.Create(
                    transactionId: Id,
                    responsibleUserId: g.Key,
                    itemsCount: g.Count(),
                    totalAmount: g.Sum(x => x.AmountCollected)))
                .ToList();

            _lines.AddRange(groups);

            RecalculateTotals();
        }

        private void RecalculateTotals()
        {
            TotalItemsCollected = _tickets.Count;
            TotalAmountCollected = _tickets.Sum(x => x.AmountCollected);
        }
    }
}