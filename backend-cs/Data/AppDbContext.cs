using backend_cs.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_cs.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Artist> Artists { get; set; }
        public DbSet<Album> Albums { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Mp3MetaData> Mp3MetaDatas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configuration for the many-to-many relationship using the junction table Mp3Genre
            modelBuilder.Entity<Mp3MetaData>()
                .HasMany(m => m.Genres)
                .WithMany(g => g.Mp3s)
                .UsingEntity<Dictionary<string, object>>(
                    "Mp3Genre",
                    j => j.HasOne<Genre>().WithMany().HasForeignKey("GenreId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Mp3MetaData>().WithMany().HasForeignKey("Mp3Id").OnDelete(DeleteBehavior.Cascade)
                );
                
            modelBuilder.Entity<Artist>()
                .HasIndex(a => a.Name)
                .IsUnique();
                
            modelBuilder.Entity<Album>()
                .HasIndex(a => new { a.Name, a.ArtistId })
                .IsUnique();
                
            modelBuilder.Entity<Genre>()
                .HasIndex(g => g.Name)
                .IsUnique();
                
            modelBuilder.Entity<Mp3MetaData>()
                .HasIndex(m => m.FilePath)
                .IsUnique();
        }
    }
}
