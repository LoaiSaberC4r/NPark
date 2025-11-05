using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Configuration
{
    public class PricingSchemeConfiguration : IEntityTypeConfiguration<PricingScheme>
    {
        public void Configure(EntityTypeBuilder<PricingScheme> builder)
        {
            builder.ToTable("PricingSchemes");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
            builder.Property(x => x.DurationType).IsRequired();
            builder.Property(x => x.StartTime).IsRequired(false);
            builder.Property(x => x.EndTime).IsRequired(false);
            builder.Property(x => x.Salary).IsRequired();
            builder.Property(x => x.IsRepeated).IsRequired();
            builder.Property(x => x.TotalDays).IsRequired(false);
            builder.Property(x => x.TotalHours).IsRequired(false);
        }
    }
}