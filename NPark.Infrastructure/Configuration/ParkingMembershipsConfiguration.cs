using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Configuration
{
    public class ParkingMembershipsConfiguration : IEntityTypeConfiguration<ParkingMemberships>
    {
        public void Configure(EntityTypeBuilder<ParkingMemberships> builder)
        {
            builder.ToTable("ParkingMemberships");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Phone).IsRequired().HasMaxLength(15);
            builder.Property(x => x.NationalId).IsRequired();

            builder.Property(x => x.VehicleNumber).IsRequired().HasMaxLength(20);
            builder.Property(x => x.CardNumber).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.EndDate).IsRequired();
            builder.HasOne(x => x.PricingScheme)
                   .WithMany(x => x.ParkingMemberships)
                   .HasForeignKey(x => x.PricingSchemeId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Attachments)
                   .WithOne(x => x.ParkingMemberships)
                   .HasForeignKey(x => x.ParkingMembershipsId);
        }
    }
}