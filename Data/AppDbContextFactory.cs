using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SGtest.Config;

namespace SGtest.Data;

// for migrations only

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var appSettings = ConfigurationManager.LoadConfiguration();
            
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(appSettings.Database.GetConnectionString());
            
        return new AppDbContext(optionsBuilder.Options);
    }
}