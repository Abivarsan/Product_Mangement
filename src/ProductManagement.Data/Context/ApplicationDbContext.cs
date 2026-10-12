using Microsoft.EntityFrameworkCore;
using ProductManagement.Core.Entities;

namespace ProductManagement.Data.Context;

/// <summary>
/// EF Core database context for Product Management.
/// Configures tables, stored procedure keyless entities, precision constraints, and initial seed data.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<CategoryAveragePriceReport> CategoryAveragePrices => Set<CategoryAveragePriceReport>();
    public DbSet<HighestStockValueCategoryReport> HighestStockValueCategories => Set<HighestStockValueCategoryReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Fluent API Configuration for Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.Category)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(p => p.Price)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(p => p.Stock)
                .IsRequired();

            entity.Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Index category for rapid grouping and querying
            entity.HasIndex(p => p.Category)
                .HasDatabaseName("IX_Products_Category");
        });

        // 2. Keyless Entity Types for Stored Procedure projections
        modelBuilder.Entity<CategoryAveragePriceReport>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(null);
            entity.Property(e => e.AveragePrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<HighestStockValueCategoryReport>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(null);
            entity.Property(e => e.TotalStockValue).HasPrecision(18, 2);
        });

        // 3. Seed Realistic Sample Data
        SeedInitialData(modelBuilder);
    }

    private static void SeedInitialData(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Dell XPS 15 Laptop", Category = "Electronics", Price = 1799.99m, Stock = 15, CreatedAt = seedDate },
            new Product { Id = 2, Name = "Sony WH-1000XM5 Headphones", Category = "Electronics", Price = 399.99m, Stock = 42, CreatedAt = seedDate },
            new Product { Id = 3, Name = "Logitech MX Master 3S Mouse", Category = "Electronics", Price = 99.99m, Stock = 80, CreatedAt = seedDate },
            new Product { Id = 4, Name = "Ergonomic Mesh Office Chair", Category = "Furniture", Price = 249.50m, Stock = 25, CreatedAt = seedDate },
            new Product { Id = 5, Name = "Solid Oak Standing Desk", Category = "Furniture", Price = 649.00m, Stock = 12, CreatedAt = seedDate },
            new Product { Id = 6, Name = "Filing Cabinet 3-Drawer", Category = "Furniture", Price = 139.99m, Stock = 18, CreatedAt = seedDate },
            new Product { Id = 7, Name = "C# 12 and .NET 8 in Action", Category = "Books", Price = 49.99m, Stock = 95, CreatedAt = seedDate },
            new Product { Id = 8, Name = "Clean Architecture Patterns", Category = "Books", Price = 44.95m, Stock = 60, CreatedAt = seedDate },
            new Product { Id = 9, Name = "Cotton Crew Neck T-Shirt", Category = "Apparel", Price = 19.99m, Stock = 120, CreatedAt = seedDate },
            new Product { Id = 10, Name = "Waterproof Winter Parka", Category = "Apparel", Price = 189.00m, Stock = 8, CreatedAt = seedDate } // Low stock (< 10) for testing alert!
        );
    }
}
