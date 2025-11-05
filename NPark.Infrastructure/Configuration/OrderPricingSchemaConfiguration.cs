using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Configuration
{
    public sealed class OrderPricingSchemaConfiguration : IEntityTypeConfiguration<OrderPricingSchema>
    {
        public void Configure(EntityTypeBuilder<OrderPricingSchema> builder)
        {
            builder.ToTable("OrderPricingSchemas");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.PricingSchemaId).IsRequired();
            builder.Property(x => x.Count).IsRequired();
            builder.HasOne(x => x.PricingScheme).WithMany().HasForeignKey(x => x.PricingSchemaId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}