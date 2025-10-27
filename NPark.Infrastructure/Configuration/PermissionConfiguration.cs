﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Configuration
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("Permissions");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.NameAr).IsRequired().HasMaxLength(100);
            builder.Property(x => x.NameEn).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Description).HasMaxLength(500).IsRequired(false);
            builder.HasMany(x => x.RolePermissions).WithOne(x => x.Permission).HasForeignKey(x => x.PermissionId);
        }
    }
}