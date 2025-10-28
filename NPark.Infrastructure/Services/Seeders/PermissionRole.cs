using BuildingBlock.Application.Repositories;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Services.Seeders
{
    internal class PermissionRole : ISeeder
    {
        public int ExecutionOrder { get; set; } = 3;
        private readonly IGenericRepository<RolePermission> _rolePermissionRepo;
        private readonly ILogger<PermissionRole> _logger;

        public PermissionRole(IGenericRepository<RolePermission> rolePermissionRepo, ILogger<PermissionRole> logger)
        {
            _rolePermissionRepo = rolePermissionRepo ?? throw new ArgumentNullException(nameof(rolePermissionRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SeedAsync()
        {
            try
            {
                var rolePermission = new List<RolePermission>();

                rolePermission.Add(RolePermission.Create(new Guid("f47ac10b-58cc-4372-a567-0e02b2c3d479"), new Guid("d6f0b8ae-7b3f-4a32-9f38-306eec4c80ff")));
                rolePermission.Add(RolePermission.Create(new Guid("f47ac10b-58cc-4372-a567-0e02b2c3d479"), new Guid("c4f8b057-b37f-4570-bc65-d29d830fb89d")));
                rolePermission.Add(RolePermission.Create(new Guid("f47ac10b-58cc-4372-a567-0e02b2c3d479"), new Guid("17a145f1-ef9d-4f74-98ff-bab12c997b8b")));
                rolePermission.Add(RolePermission.Create(new Guid("f47ac10b-58cc-4372-a567-0e02b2c3d479"), new Guid("42a3a074-d2c4-459d-bec9-cd6e29b7b38f")));

                await _rolePermissionRepo.AddRangeAsync(rolePermission, default);
                await _rolePermissionRepo.SaveChangesAsync();
                _logger.LogInformation("PermissionRole Seeded Successfully.");
            }
            catch (Exception) { throw; }
        }
    }
}