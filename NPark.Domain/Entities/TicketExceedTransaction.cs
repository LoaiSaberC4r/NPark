using NPark.Domain.Enums;
using BuildingBlock.Domain.EntitiesHelper;

namespace NPark.Domain.Entities
{
    public sealed class TicketExceedTransaction : Entity<Guid>
    {
        private readonly List<Sanction> _sanctions = new();

        private TicketExceedTransaction()
        {
        }

        // -----------------------------
        // Ticket Snapshot - No FK to Ticket
        // -----------------------------
        public Guid TicketIdSnapshot { get; private set; }

        public string TicketUniqueCode { get; private set; } = string.Empty;
        public DateTime TicketStartDate { get; private set; }
        public DateTime? TicketEndDate { get; private set; }
        public decimal TicketPrice { get; private set; }
        public decimal TicketOldExceedPrice { get; private set; }
        public decimal TotalPriceAfterExceed { get; private set; }

        public string? VehicleNumber { get; private set; }
        public string? CardNumber { get; private set; }
        public bool IsSubscriber { get; private set; }
        public string? SubscriberNationalId { get; private set; }

        // Snapshot from Ticket itself
        public Guid TicketGateId { get; private set; }

        public Guid? TicketExitGateId { get; private set; }

        // -----------------------------
        // Transaction Data
        // -----------------------------
        public SanctionPaymentStatus PaymentStatus { get; private set; }
        public DateTime? PaymentConfirmedAtUtc { get; private set; }
        public Guid? PaymentCashierId { get; private set; }
        public Guid? PaymentGateId { get; private set; }
        public DateOnly? PenaltyFromDate { get; private set; }
        public DateOnly? PenaltyThroughDate { get; private set; }
        public SanctionClassification Classification { get; private set; }
        public string? IdempotencyKey { get; private set; }
        public string? AppliedSanctionsJson { get; private set; }
        public byte[] RowVersion { get; private set; } = [];

        public decimal ExceedAmount { get; private set; }

        public DateTime CreatedAt { get; private set; }

        // -----------------------------
        // Relations
        // -----------------------------
        public Guid GateId { get; private set; }

        public ParkingGate Gate { get; private set; } = null!;

        public Guid UserCollectId { get; private set; }
        public User UserCollector { get; private set; } = null!;

        public Guid? SuperVisorCollectorId { get; private set; }
        public User? SuperVisorCollector { get; private set; }
        public DateTime? SuperVisorCollectedAt { get; private set; }

        public IReadOnlyCollection<Sanction> Sanctions => _sanctions;

        public static TicketExceedTransaction Create(
            Ticket ticket,
            decimal oldExceedPrice,
            decimal exceedAmount,
            Guid gateId,
            Guid userCollectId,
            IReadOnlyCollection<Sanction> sanctions,
            DateTime paidAtUtc, DateTime businessTime, string idempotencyKey,
            SanctionClassification classification, DateOnly? fromDate, DateOnly? throughDate,
            string appliedSanctionsJson)
        {
            if (ticket is null)
                throw new ArgumentNullException(nameof(ticket));

            if (sanctions is null || sanctions.Count == 0)
                throw new ArgumentException("Sanctions are required.", nameof(sanctions));

            if (exceedAmount <= 0)
                throw new ArgumentOutOfRangeException(nameof(exceedAmount), "A confirmed sanction payment must be positive.");

            var transaction = new TicketExceedTransaction
            {
                Id = Guid.NewGuid(),
                PaymentStatus = SanctionPaymentStatus.Confirmed,
                PaymentConfirmedAtUtc = paidAtUtc,
                PaymentCashierId = userCollectId,
                PaymentGateId = gateId,
                PenaltyFromDate = fromDate,
                PenaltyThroughDate = throughDate,
                Classification = classification,
                IdempotencyKey = idempotencyKey,
                AppliedSanctionsJson = appliedSanctionsJson,

                TicketIdSnapshot = ticket.Id,
                TicketUniqueCode = ticket.UniqueCode,
                TicketStartDate = ticket.StartDate,
                TicketEndDate = ticket.EndDate,
                TicketPrice = ticket.Price,
                TicketOldExceedPrice = oldExceedPrice,
                ExceedAmount = exceedAmount,
                TotalPriceAfterExceed = ticket.Price + oldExceedPrice + exceedAmount,

                VehicleNumber = ticket.VehicleNumber,
                CardNumber = ticket.CardNumber,
                IsSubscriber = ticket.IsSubscriber,
                SubscriberNationalId = ticket.SubscriberNationalId,

                TicketGateId = ticket.GateId,
                TicketExitGateId = ticket.ExitGateId,

                GateId = gateId,
                UserCollectId = userCollectId,

                // Important: initially null
                SuperVisorCollectorId = null,
                SuperVisorCollectedAt = null,

                CreatedAt = businessTime
            };

            transaction._sanctions.AddRange(sanctions);

            return transaction;
        }

        public void SetSupervisorCollected(Guid supervisorId, DateTime? now)
        {
            if (supervisorId == Guid.Empty)
                throw new ArgumentException("Supervisor id is required.", nameof(supervisorId));

            if (SuperVisorCollectorId.HasValue)
                throw new InvalidOperationException("Ticket exceed transaction is already collected by supervisor.");

            SuperVisorCollectorId = supervisorId;
            SuperVisorCollectedAt = now ?? DateTime.Now;
        }
    }
}
