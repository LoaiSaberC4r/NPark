using BuildingBlock.Infrastracture.Extensions;
using Microsoft.EntityFrameworkCore;
using NPark.Domain.Entities;

namespace NPark.Infrastructure
{
    public class NParkDBContext : DbContext
    {
        public NParkDBContext(DbContextOptions<NParkDBContext> options)
: base(options) { }

        public DbSet<LprCapture> LprCaptures => Set<LprCapture>();
        public DbSet<LprCaptureArchive> LprCapturesArchive => Set<LprCaptureArchive>();
        public DbSet<ParkingHardwareEvent> ParkingHardwareEvents => Set<ParkingHardwareEvent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasSequence<long>("GateCommandIds").StartsAt(1).HasMin(1).HasMax(long.MaxValue);
            modelBuilder.ApplySoftDeleteQueryFilter();
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(NParkDBContext).Assembly);
        }
    }
}
