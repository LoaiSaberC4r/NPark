using BuildingBlock.Domain.EntitiesHelper;
using NPark.Domain.Enums;
using System.Buffers.Binary;
using System.Globalization;

namespace NPark.Domain.Entities
{
    public sealed class Ticket : AggregateRoot<Guid>
    {
        private readonly List<TicketAttachment> _ticketAttachments = new();
        public DateOnly ValidExitDate { get; private set; }
        public EntryConfirmationStatus EntryConfirmationStatus { get; private set; }
        public DateTime? EntryConfirmedAtUtc { get; private set; }
        public BasePaymentStatus BasePaymentStatus { get; private set; }
        public DateTime? BasePaymentConfirmedAtUtc { get; private set; }
        public Guid? BasePaymentCashierId { get; private set; }
        public Guid? BasePaymentGateId { get; private set; }
        public PriceType? PricingModeSnapshot { get; private set; }
        public byte[] RowVersion { get; private set; } = [];
        public bool BasePriceCalculated { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime? EndDate { get; private set; } = null;
        public decimal Price { get; private set; }
        public string? VehicleNumber { get; private set; } = null;
        public decimal ExceedPrice { get; private set; } = 0;
        public Guid? CollectedBy { get; private set; }
        public decimal TotalPrice => Price + ExceedPrice;
        public bool IsCollected { get; private set; }
        public DateTime? CollectedDate { get; private set; }
        public byte[] UniqueGuidPart { get; private set; }
        public string UniqueCode { get; private set; }
        public Guid GateId { get; private set; }
        public Guid? ExitGateId { private set; get; }
        public Guid UserId { get; private set; }
        public bool IsCashierCollected { get; private set; } = false;
        public User User { get; private set; }
        public User? UserCollector { get; private set; }
        public ParkingGate ParkingGate { get; private set; }
        public bool IsSubscriber { get; private set; } = false;
        public string? SubscriberNationalId { get; private set; }
        public string? CardNumber { get; private set; } = null;
        public IReadOnlyCollection<TicketAttachment> TicketAttachments => _ticketAttachments;

        private Ticket()
        { }

        public static Ticket Create(DateTime startDate, decimal price, Guid gateId, Guid userId,
            DateOnly businessDate, PriceType pricingMode)
        {
            var ticket = new Ticket()
            {
                Id = Guid.NewGuid(),
                StartDate = startDate,
                ValidExitDate = businessDate,
                PricingModeSnapshot = pricingMode,
                BasePriceCalculated = pricingMode == PriceType.Enter,
                BasePaymentStatus = BasePaymentStatus.Unpaid,
                EntryConfirmationStatus = EntryConfirmationStatus.Pending,
                Price = price,
                GateId = gateId,
                UserId = userId,
            };

            // Save the first 4 bytes of the Guid as the unique part
            ticket.UniqueGuidPart = GetGuidUniquePart(ticket.Id);
            ticket.UniqueCode = BitConverter.ToString(ticket.UniqueGuidPart).Replace("-", "");

            return ticket;
        }

        public void SetCollected(Guid Id, DateTime? now)
        {
            IsCollected = true;
            CollectedBy = Id;
            CollectedDate = now ?? DateTime.Now;
        }

        public void SetVehicleNumber(string vehicleNumber) => VehicleNumber = vehicleNumber;

        public void ConfirmEntry(DateTime utc)
        {
            if (EntryConfirmationStatus == EntryConfirmationStatus.Confirmed)
                return;
            EntryConfirmedAtUtc = utc;
            EntryConfirmationStatus = EntryConfirmationStatus.Confirmed;
        }

        public void ConfirmBasePayment(Guid cashierId, Guid gateId, DateTime utc)
        {
            if (IsCashierCollected)
                return;
            if (EndDate.HasValue || !BasePriceCalculated && BasePaymentStatus != BasePaymentStatus.Legacy)
                throw new InvalidOperationException("The ticket has no collectible base quote.");

            IsCashierCollected = true;
            BasePaymentStatus = Price == 0 ? BasePaymentStatus.NoCharge : BasePaymentStatus.Paid;
            if (Price > 0)
            {
                BasePaymentCashierId = cashierId;
                BasePaymentGateId = gateId;
                BasePaymentConfirmedAtUtc = utc;
            }
        }

        public void ExtendExitValidity(DateOnly date)
        {
            if (date > ValidExitDate)
                ValidExitDate = date;
        }

        public void CompleteExit(Guid gateId, DateTime confirmedAt)
        {
            if (gateId == Guid.Empty || EndDate.HasValue)
                throw new InvalidOperationException("A valid gate and an active ticket are required.");
            ExitGateId = gateId;
            EndDate = confirmedAt;
        }

        public void SetCollectedBy(Guid collectedBy)
        {
            CollectedBy = collectedBy;
            IsCollected = true;
            CollectedDate = DateTime.Now;
        }

        private static byte[] GetGuidUniquePart(Guid guid)
        {
            var first8 = guid.ToString("N").AsSpan(0, 8);
            var value = int.Parse(first8, NumberStyles.HexNumber);
            Span<byte> buf = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(buf, value);
            return buf.ToArray();
        }

        public void SetSubscriber(string nationalId, string? vehicleNumber = null, string? cardNumber = null)
        {
            Price = 0;
            IsSubscriber = true;
            BasePriceCalculated = true;
            IsCashierCollected = true;
            BasePaymentStatus = BasePaymentStatus.NoCharge;
            VehicleNumber = vehicleNumber;
            SubscriberNationalId = nationalId;
            CardNumber = cardNumber;
        }

        public void SetPrice(decimal price)
        {
            if (IsCashierCollected || EndDate.HasValue || price < 0)
                throw new InvalidOperationException("The base price cannot be changed.");
            Price = price;
            BasePriceCalculated = true;
        }

        public static byte[] ParseUniqueCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Invalid ticket code.", nameof(code));

            // "63729C54" -> byte[4]
            return Convert.FromHexString(code);
        }

        public void AddAttachment(string filePath)
        {
            var attachment = TicketAttachment.Create(Id, filePath);
            _ticketAttachments.Add(attachment);
        }

        public void AddExceedPrice(decimal exceedPrice)
        {
            if (exceedPrice < 0)
                throw new ArgumentOutOfRangeException(nameof(exceedPrice), "Exceed price cannot be negative.");

            ExceedPrice += exceedPrice;
        }
    }
}