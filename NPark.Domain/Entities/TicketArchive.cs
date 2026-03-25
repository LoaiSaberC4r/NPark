using BuildingBlock.Domain.EntitiesHelper;

namespace NPark.Domain.Entities
{
    public sealed class TicketArchive : Entity<Guid>
    {
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

        private TicketArchive()
        { }

        public static TicketArchive CreateFromTicket(Ticket ticket)
        {
            return new TicketArchive
            {
                Id = ticket.Id,
                StartDate = ticket.StartDate,
                EndDate = ticket.EndDate ?? DateTime.Now,
                Price = ticket.Price,
                VehicleNumber = ticket.VehicleNumber,
                ExceedPrice = ticket.ExceedPrice,
                CollectedBy = ticket.CollectedBy,
                IsCollected = ticket.IsCollected,
                CollectedDate = ticket.CollectedDate is not null ? ticket.CollectedDate.Value : DateTime.Now,
                UniqueGuidPart = ticket.UniqueGuidPart,
                UniqueCode = ticket.UniqueCode,
                GateId = ticket.GateId,
                ExitGateId = ticket.ExitGateId is not null ? ticket.ExitGateId.Value : Guid.Empty,
                UserId = ticket.UserId,
                IsCashierCollected = ticket.IsCashierCollected,
                User = ticket.User,
                UserCollector = ticket.UserCollector,
                ParkingGate = ticket.ParkingGate,
                IsSubscriber = ticket.IsSubscriber,
                SubscriberNationalId = ticket.SubscriberNationalId,
                CardNumber = ticket.CardNumber
            };
        }

        public void SetCollected(Guid Id)
        {
            IsCollected = true;
            CollectedBy = Id;
            CollectedDate = DateTime.Now;
        }
    }
}