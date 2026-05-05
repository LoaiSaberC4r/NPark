using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Configuration
{
    internal sealed class TicketArchiveAttachmentConfiguration
       : IEntityTypeConfiguration<TicketArchiveAttachment>
    {
        public void Configure(EntityTypeBuilder<TicketArchiveAttachment> builder)
        {
            builder.ToTable("TicketArchiveAttachments");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.FilePath)
                .IsRequired()
                .HasMaxLength(500);

            builder.HasOne(x => x.TicketArchive)
                .WithMany(x => x.TicketArchiveAttachments)
                .HasForeignKey(x => x.TicketArchiveId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.TicketArchiveId);
        }
    }
}