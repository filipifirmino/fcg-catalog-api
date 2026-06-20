using FCG_CATALOG_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCG_CATALOG_API.Infra
{
    public class AppDbContext : DbContext
    {
        public DbSet<Game> Games => Set<Game>();
        public DbSet<Acquisition> Acquisitions => Set<Acquisition>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Game>(entity =>
            {
                entity.Property(g => g.Title).IsRequired().HasMaxLength(200);
                entity.Property(g => g.Price).IsRequired();
                entity.Property(g => g.Genre).HasMaxLength(100);
                entity.Property(g => g.Slug).IsRequired().HasMaxLength(200);
            });

            modelBuilder.Entity<Acquisition>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.UserId).IsRequired();
                entity.Property(a => a.GameId).IsRequired();
                entity.Property(a => a.PricePaid).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(a => a.AcquisitionDate).IsRequired();

                entity.HasOne<Game>()
                      .WithMany()
                      .HasForeignKey(a => a.GameId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
