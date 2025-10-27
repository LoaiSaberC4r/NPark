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

        public IUserSeeder(IGenericRepository<User> userRepo, ILogger<IUserSeeder> logger)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public int ExecutionOrder { get; set; } = 1;

        public Task SeedAsync()
        {
            throw new NotImplementedException();
        }
    }
}