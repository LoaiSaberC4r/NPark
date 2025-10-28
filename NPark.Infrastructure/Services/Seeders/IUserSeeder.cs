using BuildingBlock.Application.Abstraction.Encryption;
using BuildingBlock.Application.Repositories;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Services.Seeders
{
    internal class IUserSeeder : ISeeder
    {
        private readonly IGenericRepository<User> _userRepo;
        private readonly ILogger<IUserSeeder> _logger;
        private readonly IPasswordService _passwordService;

        public IUserSeeder(IGenericRepository<User> userRepo, ILogger<IUserSeeder> logger, IPasswordService passwordService)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        }

        public int ExecutionOrder { get; set; } = 4;

        public async Task SeedAsync()
        {
            var password = _passwordService.Hash("Admin123");
            var user = User.Create("Admin", "Admin@gmail.com", "Admin", password, "01004117696", "2650221627841");
            user.SetRole(new Guid("f47ac10b-58cc-4372-a567-0e02b2c3d479"));
            await _userRepo.AddAsync(user, default);
            await _userRepo.SaveChangesAsync();
            _logger.LogInformation("Users Seeded Successfully.");
        }
    }
}