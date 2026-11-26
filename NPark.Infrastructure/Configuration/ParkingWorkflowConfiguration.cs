using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPark.Domain.Entities;
using NPark.Domain.Enums;

namespace NPark.Infrastructure.Configuration;

internal sealed class GateOperationConfiguration : IEntityTypeConfiguration<GateOperation>
{
    public void Configure(EntityTypeBuilder<GateOperation> builder)
    {
        builder.ToTable("GateOperations", table =>
            table.HasCheckConstraint("CK_GateOperations_Controller", "[ControllerId] BETWEEN 1 AND 65535 AND [CommandId] > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CommandId).HasDefaultValueSql("NEXT VALUE FOR [GateCommandIds]").ValueGeneratedOnAdd();
        builder.Property(x => x.CompletionMode).HasDefaultValue(GateOperationCompletionMode.Hardware);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Property(x => x.ReconciliationEvidence).HasMaxLength(500);
        builder.Property(x => x.ErrorCode).HasMaxLength(64);
        builder.HasIndex(x => x.CommandId).IsUnique();
        builder.HasIndex(x => new { x.TicketIdSnapshot, x.Type });
        builder.HasIndex(x => x.ControllerId);
        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });
        builder.HasIndex(x => x.ReservedTicketId).IsUnique().HasFilter("[ReservedTicketId] IS NOT NULL");
        builder.HasIndex(x => x.ReservedControllerId).IsUnique().HasFilter("[ReservedControllerId] IS NOT NULL");
    }
}

internal sealed class OvernightPenaltyCoverageConfiguration : IEntityTypeConfiguration<OvernightPenaltyCoverage>
{
    public void Configure(EntityTypeBuilder<OvernightPenaltyCoverage> builder)
    {
        builder.ToTable("OvernightPenaltyCoverages");
        builder.HasKey(x => new { x.TicketIdSnapshot, x.BusinessDate });
        builder.HasOne<TicketExceedTransaction>().WithMany().HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed record TransactionReceipt(Guid Id);

internal sealed class TransactionReceiptConfiguration : IEntityTypeConfiguration<TransactionReceipt>
{
    public void Configure(EntityTypeBuilder<TransactionReceipt> builder)
    {
        builder.ToTable("TransactionReceipts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
    }
}
