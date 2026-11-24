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
    internal sealed class TicketCollectionTransactionLineConfiguration
      : IEntityTypeConfiguration<TicketCollectionTransactionLine>
    {
        public void Configure(EntityTypeBuilder<TicketCollectionTransactionLine> builder)
        {
            builder.ToTable("TicketCollectionTransactionLines");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.TicketCollectionTransactionId)
                .IsRequired();

            builder.Property(x => x.ResponsibleUserId)
                .IsRequired();

            builder.Property(x => x.ItemsCount)
                .IsRequired();

            builder.Property(x => x.TotalAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.HasOne(x => x.ResponsibleUser)
                .WithMany()
                .HasForeignKey(x => x.ResponsibleUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new
            {
                x.TicketCollectionTransactionId,
                x.ResponsibleUserId
            })
            .IsUnique();
        }
    }
}