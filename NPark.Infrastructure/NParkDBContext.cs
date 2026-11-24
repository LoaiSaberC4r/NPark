using BuildingBlock.Infrastracture.Extensions;
using Microsoft.EntityFrameworkCore;

namespace NPark.Infrastructure
{
    public class NParkDBContext : DbContext
    {
        public NParkDBContext(DbContextOptions<NParkDBContext> options)
: base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasSequence<long>("GateCommandIds").StartsAt(1).HasMin(1).HasMax(long.MaxValue);
            modelBuilder.ApplySoftDeleteQueryFilter();
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(NParkDBContext).Assembly);
        }
    }
}