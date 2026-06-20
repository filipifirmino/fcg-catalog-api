using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FCG_CATALOG_API.Infra;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=fcg_catalog_db;Username=postgree;Password=postgree")
                      .UseSnakeCaseNamingConvention();
        return new AppDbContext(optionsBuilder.Options);
    }
}
