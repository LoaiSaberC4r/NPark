using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Configuration;

internal sealed class LprCaptureConfiguration : IEntityTypeConfiguration<LprCapture>
{
    public void Configure(EntityTypeBuilder<LprCapture> builder)
    {
        builder.ToTable("LprCaptures");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.LprIp).HasMaxLength(100);
        builder.Property(x => x.VehicleNumber).HasMaxLength(50);
        builder.Property(x => x.NormalizedVehicleNumber).HasMaxLength(50);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.ControllerId, x.GateId, x.CapturedAtUtc, x.Id })
            .IsDescending(false, false, true, true);
        builder.HasIndex(x => new { x.GateId, x.CapturedAtUtc });
        builder.HasIndex(x => new { x.GateId, x.NormalizedVehicleNumber, x.CapturedAtUtc });
        builder.HasIndex(x => new { x.CapturedAtUtc, x.Id });
        builder.HasIndex(x => x.TicketId).HasFilter("[TicketId] IS NOT NULL");
    }
}

internal sealed class LprCaptureArchiveConfiguration : IEntityTypeConfiguration<LprCaptureArchive>
{
    public void Configure(EntityTypeBuilder<LprCaptureArchive> builder)
    {
        builder.ToTable("LprCapturesArchive");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.LprIp).HasMaxLength(100);
        builder.Property(x => x.VehicleNumber).HasMaxLength(50);
        builder.Property(x => x.NormalizedVehicleNumber).HasMaxLength(50);
    }
}

internal sealed class ParkingHardwareEventConfiguration : IEntityTypeConfiguration<ParkingHardwareEvent>
{
    public void Configure(EntityTypeBuilder<ParkingHardwareEvent> builder)
    {
        builder.ToTable("ParkingHardwareEvents", table =>
            table.HasCheckConstraint("CK_ParkingHardwareEvents_Controller", "[ControllerId] BETWEEN 1 AND 65535"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.EventId).HasColumnType("binary(8)");
        builder.Property(x => x.RawPayload).HasMaxLength(31);
        builder.Property(x => x.CardNumber).HasMaxLength(8).IsUnicode(false);
        builder.Property(x => x.FailureReasonCode).HasMaxLength(100).IsUnicode(false);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.ControllerId, x.EventId }).IsUnique();
        builder.HasIndex(x => new { x.ProcessingStatus, x.ReceivedAtUtc, x.Id });
        builder.HasIndex(x => new { x.ReceivedAtUtc, x.Id }).HasFilter("[StorageAckSentAtUtc] IS NULL");
        // These IDs are historical references. Captures and tickets can move to their archive tables.
        builder.HasIndex(x => x.LprCaptureId).IsUnique().HasFilter("[LprCaptureId] IS NOT NULL");
        builder.HasIndex(x => x.TicketId).IsUnique().HasFilter("[TicketId] IS NOT NULL");
    }
}
