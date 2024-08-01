using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace NetScrapy;

public class ScrapedDataContext : DbContext
{
    public DbSet<ScrapedDataModel> ScrapedData { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=ScrapedDataDB;Trusted_Connection=True;TrustServerCertificate=True;");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScrapedDataModel>()
            .HasKey(sd => sd.Id);

        modelBuilder.Entity<ScrapedDataModel>()
            .Property(sd => sd.Elements)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null!)
            );

        modelBuilder.Entity<ScrapedDataModel>()
            .Property(sd => sd.Created);
    }
}