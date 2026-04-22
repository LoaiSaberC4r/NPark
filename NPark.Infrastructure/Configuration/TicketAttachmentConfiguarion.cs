using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Configuration
{
    internal sealed class TicketAttachmentConfiguarion : IEntityTypeConfiguration<TicketAttachment>
    {
        public void Configure(EntityTypeBuilder<TicketAttachment> builder)
        {
            builder.ToTable("TicketAttachments");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.FilePath).IsRequired();

            builder.HasOne(x => x.Ticket)
                .WithMany(x => x.TicketAttachments).HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}