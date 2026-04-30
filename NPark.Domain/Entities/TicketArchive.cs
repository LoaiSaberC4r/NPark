using BuildingBlock.Domain.EntitiesHelper;

namespace NPark.Domain.Entities
{
    public sealed class TicketArchive : Entity<Guid>
    {
        private readonly List<TicketArchiveAttachment> _ticketArchiveAttachments = new();

        public DateTime StartDate { get; private set; }

        public DateTime EndDate { get; private set; }

        public decimal Price { get; private set; }

        public string? VehicleNumber { get; private set; } = null;

        public decimal ExceedPrice { get; private set; } = 0;

        public Guid? CollectedBy { get; private set; }

        public decimal TotalPrice => Price + ExceedPrice;

        public bool IsCollected { get; private set; }

        public DateTime CollectedDate { get; private set; }

        public byte[] UniqueGuidPart { get; private set; } = Array.Empty<byte>();

        public string UniqueCode { get; private set; } = string.Empty;

        public Guid GateId { get; private set; }

        public Guid ExitGateId { private set; get; }

        public Guid UserId { get; private set; }

        public bool IsCashierCollected { get; private set; } = false;

        public User User { get; private set; } = default!;

        public User? UserCollector { get; private set; }

        public ParkingGate ParkingGate { get; private set; } = default!;

        public bool IsSubscriber { get; private set; } = false;

        public string? SubscriberNationalId { get; private set; }

        public string? CardNumber { get; private set; } = null;

        public IReadOnlyCollection<TicketArchiveAttachment> TicketArchiveAttachments
            => _ticketArchiveAttachments.AsReadOnly();

        private TicketArchive()
        {
        }

        public static TicketArchive CreateFromTicket(Ticket ticket)
        {
            ArgumentNullException.ThrowIfNull(ticket);

            var ticketArchive = new TicketArchive
            {
                Id = ticket.Id,
                StartDate = ticket.StartDate,
                EndDate = ticket.EndDate ?? DateTime.Now,
                Price = ticket.Price,
                VehicleNumber = ticket.VehicleNumber,
                ExceedPrice = ticket.ExceedPrice,
                CollectedBy = ticket.CollectedBy,
                IsCollected = ticket.IsCollected,
                CollectedDate = ticket.CollectedDate ?? DateTime.Now,
                UniqueGuidPart = ticket.UniqueGuidPart,
                UniqueCode = ticket.UniqueCode,
                GateId = ticket.GateId,
                ExitGateId = ticket.ExitGateId ?? Guid.Empty,
                UserId = ticket.UserId,
                IsCashierCollected = ticket.IsCashierCollected,

                // Existing navigation references.
                // EF will save using FK values above.
                User = ticket.User,
                UserCollector = ticket.UserCollector,
                ParkingGate = ticket.ParkingGate,

                IsSubscriber = ticket.IsSubscriber,
                SubscriberNationalId = ticket.SubscriberNationalId,
                CardNumber = ticket.CardNumber
            };

            ticketArchive.CopyAttachmentsFromTicket(ticket);

            return ticketArchive;
        }

        public void SetCollected(Guid id)
        {
            IsCollected = true;
            CollectedBy = id;
            CollectedDate = DateTime.Now;
        }

        private void CopyAttachmentsFromTicket(Ticket ticket)
        {
            if (ticket.TicketAttachments is null || ticket.TicketAttachments.Count == 0)
                return;

            foreach (var attachment in ticket.TicketAttachments)
            {
                AddAttachmentSnapshot(attachment.FilePath);
            }
        }

        private void AddAttachmentSnapshot(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            var archiveAttachment = TicketArchiveAttachment.Create(Id, filePath);
            _ticketArchiveAttachments.Add(archiveAttachment);
        }
    }
}