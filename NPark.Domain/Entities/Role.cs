﻿using BuildingBlock.Domain.EntitiesHelper;

namespace NPark.Domain.Entities
{
    public class Role : Entity<Guid>
    {
        private List<RolePermission> Permissions { get; set; } = new List<RolePermission>(); // <Role, Permission>
        public string NameEn { get; private set; } = string.Empty;
        public string NameAr { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;

        public IReadOnlyCollection<RolePermission> GetPermissions => Permissions.AsReadOnly();

        private Role()
        { }

        public static Role Create(string nameEn, string nameAr, string description) => new Role()
        {
            NameEn = nameEn,
            Description = description,
            NameAr = nameAr
        };

        public static Role CreateWithID(Guid id, string nameEn, string nameAr, string description) => new Role()
        {
            Id = id,
            NameEn = nameEn,
            Description = description,
            NameAr = nameAr
        };

        public void UpdateNameEn(string name) => NameEn = name;

        public void UpdateNameAr(string name) => NameAr = name;

        public void UpdateDescription(string description) => Description = description;
    }
}