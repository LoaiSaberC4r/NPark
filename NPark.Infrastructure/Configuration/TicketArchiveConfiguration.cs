using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Configuration
{
    internal sealed class TicketArchiveConfiguration : IEntityTypeConfiguration<TicketArchive>
    {
        public void Configure(EntityTypeBuilder<TicketArchive> builder)
        {
            builder.Property(x => x.RowVersion).IsRowVersion();
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id)
    .ValueGeneratedNever();
            builder.Property(t => t.StartDate).IsRequired();
            builder.Property(t => t.EndDate).IsRequired();
            builder.Property(t => t.CollectedBy).IsRequired(false);
            builder.Property(t => t.ExitGateId).IsRequired();
            builder.Property(t => t.VehicleNumber).IsRequired(false);
            builder.Property(t => t.CollectedDate).IsRequired(false);
            builder.Property(t => t.UniqueCode).IsRequired(false);
            builder.Property(t => t.Price).IsRequired();
            builder.Property(t => t.IsSubscriber).IsRequired().HasDefaultValue(false);
            builder.Property(t => t.SubscriberNationalId).IsRequired(false);

            builder.Property(t => t.IsCashierCollected).IsRequired().HasDefaultValue(false);
            builder.Property(t => t.UniqueGuidPart)
                  .IsRequired()
                  .HasColumnType("BINARY(4)");
            builder.Property(t => t.ExceedPrice).IsRequired().HasDefaultValue(0);
            builder.Property(t => t.IsCollected).IsRequired().HasDefaultValue(true);
            builder.HasIndex(x => x.UniqueGuidPart).IsUnique();

            builder.HasOne(x => x.ParkingGate)
                .WithMany()
                .HasForeignKey(x => x.GateId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.UserCollector)
                .WithMany()
                .HasForeignKey(x => x.CollectedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasOne(x => x.ExitParkingGate)
              .WithMany()
              .HasForeignKey(x => x.ExitGateId)
              .OnDelete(DeleteBehavior.Restrict);
        }
    }
}