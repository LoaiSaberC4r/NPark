using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Configuration
{
    internal sealed class TicketExceedTransactionConfiguration
        : IEntityTypeConfiguration<TicketExceedTransaction>
    {
        public void Configure(EntityTypeBuilder<TicketExceedTransaction> builder)
        {
            builder.ToTable("TicketExceedTransactions");

            builder.HasKey(x => x.Id);

            // -----------------------------
            // Ticket Snapshot - No FK to Ticket
            // -----------------------------
            builder.Property(x => x.TicketIdSnapshot)
                .IsRequired();

            builder.Property(x => x.TicketUniqueCode)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.TicketStartDate)
                .IsRequired()
                .HasColumnType("datetime2");

            builder.Property(x => x.TicketEndDate)
                .HasColumnType("datetime2");

            builder.Property(x => x.TicketPrice)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.TicketOldExceedPrice)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.ExceedAmount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.TotalPriceAfterExceed)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.VehicleNumber)
                .HasMaxLength(50);

            builder.Property(x => x.CardNumber)
                .HasMaxLength(50);

            builder.Property(x => x.SubscriberNationalId)
                .HasMaxLength(50);

            builder.Property(x => x.IsSubscriber)
                .IsRequired();

            builder.Property(x => x.TicketGateId)
                .IsRequired();

            builder.Property(x => x.TicketExitGateId)
                .IsRequired(false);

            builder.Property(x => x.CreatedAt)
                .IsRequired()
                .HasColumnType("datetime2");

            builder.Property(x => x.SuperVisorCollectedAt)
              .HasColumnType("datetime2")
              .IsRequired(false);

            // -----------------------------
            // Gate Relation
            // -----------------------------
            builder.HasOne(x => x.Gate)
                .WithMany()
                .HasForeignKey(x => x.GateId)
                .OnDelete(DeleteBehavior.Restrict);

            // -----------------------------
            // User Collector Relation
            // -----------------------------
            builder.HasOne(x => x.UserCollector)
                .WithMany()
                .HasForeignKey(x => x.UserCollectId)
                .OnDelete(DeleteBehavior.Restrict);

            // -----------------------------
            // Supervisor Collector Relation - Nullable
            // -----------------------------
            builder.HasOne(x => x.SuperVisorCollector)
                .WithMany()
                .HasForeignKey(x => x.SuperVisorCollectorId)
                .OnDelete(DeleteBehavior.Restrict);

            // -----------------------------
            // Many-to-Many with Sanctions
            // -----------------------------
            builder.HasMany(x => x.Sanctions)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "TicketExceedTransactionSanctions",
                    right => right
                        .HasOne<Sanction>()
                        .WithMany()
                        .HasForeignKey("SanctionId")
                        .OnDelete(DeleteBehavior.Restrict),
                    left => left
                        .HasOne<TicketExceedTransaction>()
                        .WithMany()
                        .HasForeignKey("TicketExceedTransactionId")
                        .OnDelete(DeleteBehavior.Restrict),
                    join =>
                    {
                        join.ToTable("TicketExceedTransactionSanctions");

                        join.HasKey("TicketExceedTransactionId", "SanctionId");

                        join.HasIndex("SanctionId");
                    });

            builder.Navigation(x => x.Sanctions)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(x => x.TicketIdSnapshot)
                .IsUnique();

            builder.HasIndex(x => x.GateId);
            builder.HasIndex(x => x.UserCollectId);
            builder.HasIndex(x => x.SuperVisorCollectorId);
            builder.HasIndex(x => x.CreatedAt);
        }
    }
}