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
            modelBuilder.ApplySoftDeleteQueryFilter();
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(NParkDBContext).Assembly);
        }
    }
}