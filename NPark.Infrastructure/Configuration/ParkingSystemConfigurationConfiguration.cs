using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Configuration
{
    public class ParkingSystemConfigurationConfiguration : IEntityTypeConfiguration<ParkingSystemConfiguration>
    {
        public void Configure(EntityTypeBuilder<ParkingSystemConfiguration> builder)
        {
            builder.ToTable("ParkingSystemConfigurations");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.EntryGatesCount).IsRequired();
            builder.Property(x => x.ExitGatesCount).IsRequired();
            builder.Property(x => x.AllowedParkingSlots).IsRequired(false);
            builder.Property(x => x.PriceType).IsRequired();
            builder.Property(x => x.GracePeriod).IsRequired(false);
            builder.Property(x => x.VehiclePassengerData).IsRequired();
            builder.Property(x => x.PrintType).IsRequired();
            builder.Property(x => x.DateTimeFlag).IsRequired();
            builder.Property(x => x.TicketIdFlag).IsRequired();
            builder.Property(x => x.FeesFlag).IsRequired();
            builder.Property(x => x.PricingSchemaId).IsRequired(false);
            builder.HasOne(x => x.PricingScheme).WithMany().
                HasForeignKey(x => x.PricingSchemaId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}