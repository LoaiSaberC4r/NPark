using BuildingBlock.Domain.EntitiesHelper;

namespace NPark.Domain.Entities
{
    public sealed class Ticket : Entity<Guid>
    {
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public decimal Price { get; private set; }
        public decimal ExceedPrice { get; private set; } = 0;
        public decimal TotalPrice => Price + ExceedPrice;
        public bool IsCollected { get; private set; }

        private Ticket()
        { }

        public static Ticket Create(DateTime startDate, DateTime endDate, decimal price)
        {
            return new Ticket()
            {
                StartDate = startDate,
                EndDate = endDate,
                Price = price,
            };
        }

        public void SetCollected() => IsCollected = true;
    }
}