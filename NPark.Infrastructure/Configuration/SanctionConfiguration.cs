using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Configuration
{
    public sealed record SanctionConfiguration : IEntityTypeConfiguration<Sanction>
    {
        public void Configure(EntityTypeBuilder<Sanction> builder)
        {
            builder.ToTable("Sanctions");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired();
            builder.Property(x => x.Description).IsRequired(false);
            builder.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
        }
    }
}