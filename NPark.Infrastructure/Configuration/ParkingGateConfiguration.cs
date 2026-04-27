using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Configuration
{
    public class ParkingGateConfiguration : IEntityTypeConfiguration<ParkingGate>
    {
        public void Configure(EntityTypeBuilder<ParkingGate> builder)
        {
            builder.Property(x => x.GateType).IsRequired();
            builder.Property(x => x.GateNumber).IsRequired();
            builder.Property(x => x.IsOccupied).IsRequired(false);
            builder.Property(x => x.OccupiedBy).IsRequired(false);
            builder.Property(x => x.OccupiedTime).IsRequired(false);
            builder.Property(x => x.LprIp).IsRequired(false);
            builder.Property(x => x.AssignedTo).IsRequired(false);
            builder.Property(x => x.ControlIp).IsRequired(false);
            builder.Property(x => x.QrPrinterName).IsRequired(false);
            builder.Property(x => x.ReceiptPrinterName).IsRequired(false);
            builder.HasIndex(x => new { x.GateNumber, x.GateType }).IsUnique();
            builder.Property(x => x.HasReceiptPrinterName)
        .IsRequired()
        .HasDefaultValue(false);

            builder.Property(x => x.HasQrPrinterName)
                .IsRequired()
                .HasDefaultValue(false);

            builder.HasOne(x => x.AssignedToUser)
                .WithOne()
                .HasForeignKey<ParkingGate>(x => x.AssignedTo)
                .IsRequired(false);

            builder.HasOne(x => x.ONVIFCameraSetting)
                .WithMany()
                .HasForeignKey(x => x.ONVIFCameraSettingId)
                .IsRequired(false);
        }
    }
}