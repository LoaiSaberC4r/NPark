using BuildingBlock.Domain.EntitiesHelper;

namespace NPark.Domain.Entities
{
    public sealed class OrderPricingSchema : Entity<Guid>
    {
        public Guid PricingSchemaId { get; private set; }
        public PricingScheme PricingScheme { get; private set; } = null!;
        public int Count { get; set; }

        private OrderPricingSchema()
        { }

        public static OrderPricingSchema Create(Guid pricingSchemaId, int count) =>
            new OrderPricingSchema() { PricingSchemaId = pricingSchemaId, Count = count };
    }
}