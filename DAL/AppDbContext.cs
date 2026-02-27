using BLL;
using Microsoft.EntityFrameworkCore;

namespace DAL;

// defines db structure and relationships
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<GameState> GameStates { get; set; } = null!;
    public DbSet<GameConfiguration> GameConfigurations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // one-to-many relationship
        modelBuilder.Entity<GameState>()
            .HasOne(gs => gs.Configuration)
            .WithMany()
            .HasForeignKey(gs => gs.ConfigurationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}