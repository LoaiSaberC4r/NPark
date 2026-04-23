using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Configuration
{
    internal class ONVIFCameraSettingConfiguration : IEntityTypeConfiguration<ONVIFCameraSetting>
    {
        public void Configure(EntityTypeBuilder<ONVIFCameraSetting> builder)
        {
            builder.ToTable("ONVIFCameraSettings");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.UrlRequest).IsRequired().HasMaxLength(300);
            builder.Property(x => x.UserName).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Password).IsRequired().HasMaxLength(100);
        }
    }
}