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
    internal sealed class TicketCollectionTransactionConfiguration
       : IEntityTypeConfiguration<TicketCollectionTransaction>
    {
        public void Configure(EntityTypeBuilder<TicketCollectionTransaction> builder)
        {
            builder.ToTable("TicketCollectionTransactions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.Type)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.GateId)
                .IsRequired();

            builder.Property(x => x.CollectedBy)
                .IsRequired();

            builder.Property(x => x.CollectedAt)
                .IsRequired();

            builder.Property(x => x.TotalItemsCollected)
                .IsRequired();

            builder.Property(x => x.TotalAmountCollected)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.HasOne(x => x.Gate)
                .WithMany()
                .HasForeignKey(x => x.GateId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Collector)
                .WithMany()
                .HasForeignKey(x => x.CollectedBy)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Lines)
                .WithOne(x => x.Transaction)
                .HasForeignKey(x => x.TicketCollectionTransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Tickets)
                .WithOne(x => x.Transaction)
                .HasForeignKey(x => x.TicketCollectionTransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.Lines)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.Navigation(x => x.Tickets)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(x => new
            {
                x.Type,
                x.GateId,
                x.CollectedAt
            });

            builder.HasIndex(x => x.CollectedBy);
        }
    }
}