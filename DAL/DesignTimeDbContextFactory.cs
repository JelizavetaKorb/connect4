using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DAL
{
    //helps when creating migrations, since Program.cs isn't running during migrations creation
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var connectionString = $"Data Source={homeDirectory}{Path.DirectorySeparatorChar}app.db";

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite(connectionString)
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging();

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}