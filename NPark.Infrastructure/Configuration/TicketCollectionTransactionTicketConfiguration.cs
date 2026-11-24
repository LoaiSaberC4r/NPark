using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPark.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Infrastructure.Configuration
{
    internal sealed class TicketCollectionTransactionTicketConfiguration
         : IEntityTypeConfiguration<TicketCollectionTransactionTicket>
    {
        public void Configure(EntityTypeBuilder<TicketCollectionTransactionTicket> builder)
        {
            builder.ToTable("TicketCollectionTransactionTickets");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.TicketCollectionTransactionId)
                .IsRequired();

            builder.Property(x => x.ResponsibleUserId)
                .IsRequired(false);

            builder.Property(x => x.Source)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.TicketIdSnapshot)
                .IsRequired();

            builder.Property(x => x.TicketUniqueCode)
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(x => x.TicketStartDate)
                .IsRequired();

            builder.Property(x => x.TicketEndDate)
                .IsRequired(false);

            builder.Property(x => x.OriginalPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(x => x.ExceedAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(x => x.AmountCollected)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(x => x.VehicleNumber)
                .HasMaxLength(50)
                .IsRequired(false);

            builder.Property(x => x.CardNumber)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(x => x.IsSubscriber)
                .IsRequired();

            builder.Property(x => x.SubscriberNationalId)
                .HasMaxLength(50)
                .IsRequired(false);

            builder.Property(x => x.TicketGateId)
                .IsRequired();

            builder.Property(x => x.TicketExitGateId)
                .IsRequired(false);

            builder.HasOne(x => x.ResponsibleUser)
                .WithMany()
                .HasForeignKey(x => x.ResponsibleUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new
            {
                x.TicketCollectionTransactionId,
                x.Source,
                x.SourceRecordId
            })
            .IsUnique();

            builder.HasIndex(x => new { x.FinancialItemType, x.SourceRecordId })
                .IsUnique();
            builder.HasIndex(x => x.TicketIdSnapshot);
            builder.HasIndex(x => x.ResponsibleUserId);

            builder.HasIndex(x => x.TicketUniqueCode);
        }
    }
}