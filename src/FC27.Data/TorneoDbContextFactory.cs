using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FC27.Data;

/// <summary>Permite a la CLI de EF generar migraciones sin iniciar MAUI.</summary>
public sealed class TorneoDbContextFactory : IDesignTimeDbContextFactory<TorneoDbContext>
{
    public TorneoDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<TorneoDbContext>()
            .UseSqlite("Data Source=fc27-diseno.db;Foreign Keys=True").Options);
}
