using BuildingBlock.Application.Repositories;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Services.Seeders
{
    internal class PermissionSeeder : ISeeder
    {
        public int ExecutionOrder { get; set; } = 1;
        private readonly IGenericRepository<Permission> _permissionRepo;
        private readonly ILogger<PermissionSeeder> _logger;

        public PermissionSeeder(IGenericRepository<Permission> permissionRepo, ILogger<PermissionSeeder> logger)
        {
            _permissionRepo = permissionRepo ?? throw new ArgumentNullException(nameof(permissionRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SeedAsync()
        {
            var permission = new List<Permission>();
            permission.Add(Permission.CreateWithID(new Guid("d6f0b8ae-7b3f-4a32-9f38-306eec4c80ff"), "Create", "أضافة", "For Creating"));
            permission.Add(Permission.CreateWithID(new Guid("c4f8b057-b37f-4570-bc65-d29d830fb89d"), "Read", "قراءة", "For Reading"));
            permission.Add(Permission.CreateWithID(new Guid("17a145f1-ef9d-4f74-98ff-bab12c997b8b"), "Update", "تعديل", "For Updating"));
            permission.Add(Permission.CreateWithID(new Guid("42a3a074-d2c4-459d-bec9-cd6e29b7b38f"), "Delete", "حذف", "For Deleting"));
            await _permissionRepo.AddRangeAsync(permission, default);
            await _permissionRepo.SaveChangesAsync();
            _logger.LogInformation("Permissions Seeded Successfully.");
        }
    }
}