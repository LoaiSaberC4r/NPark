using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Configuration
{
    public class ParkingMembershipsAttachmentConfiguration : IEntityTypeConfiguration<ParkingMembershipsAttachment>
    {
        public void Configure(EntityTypeBuilder<ParkingMembershipsAttachment> builder)
        {
            builder.ToTable("ParkingMembershipsAttachments");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.FilePath).IsRequired();

            builder.HasOne(x => x.ParkingMemberships)
                .WithMany(x => x.Attachments).HasForeignKey(x => x.ParkingMembershipsId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}