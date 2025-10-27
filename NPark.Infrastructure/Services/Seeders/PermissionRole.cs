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
            var rolePermission = new List<RolePermission>
            {
                 RolePermission.CreateWithID(1,1),
                 RolePermission { RoleId = 1, PermissionId = 2 },
                new RolePermission { RoleId = 1, PermissionId = 3 },
                new RolePermission { RoleId = 1, PermissionId = 4 },
            };
            await _rolePermissionRepo.AddRangeAsync(rolePermission, default);
            await _rolePermissionRepo.SaveChangesAsync();
            _logger.LogInformation("PermissionRole Seeded Successfully.");
        }
    }
}