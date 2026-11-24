using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NPark.Infrastructure;

public sealed class NParkDesignTimeDbContextFactory : IDesignTimeDbContextFactory<NParkDBContext>
{
    public NParkDBContext CreateDbContext(string[] args)
    {
        // EF migration scaffolding and script generation require a provider, not a live connection.
        var connection = Environment.GetEnvironmentVariable("NPARK_EF_CONNECTION")
            ?? @"Server=(localdb)\MSSQLLocalDB;Database=NParkDesignTime;Integrated Security=true";
        return new NParkDBContext(new DbContextOptionsBuilder<NParkDBContext>().UseSqlServer(connection).Options);
    }
}
