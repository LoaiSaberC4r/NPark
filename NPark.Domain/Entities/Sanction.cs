using BuildingBlock.Domain.EntitiesHelper;

namespace NPark.Domain.Entities
{
    public sealed class Sanction : Entity<Guid>
    {
        public string Name { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public decimal Amount { get; private set; }

        private Sanction()
        { }

        public static Sanction Create(string name, string description, decimal amount)
            => new Sanction() { Name = name, Description = description, Amount = amount };

        public void Update(string name, string? description, decimal amount)
        {
            Name = name;
            Description = description;
            Amount = amount;
        }
    }
}