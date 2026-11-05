using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Manufacturing.Infrastructure;

public sealed class ManufacturingDbContextFactory : IDesignTimeDbContextFactory<ManufacturingDbContext>
{
    public ManufacturingDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Database=manufacturing_design";
        return new ManufacturingDbContext(new DbContextOptionsBuilder<ManufacturingDbContext>().UseNpgsql(connection).Options);
    }
}
