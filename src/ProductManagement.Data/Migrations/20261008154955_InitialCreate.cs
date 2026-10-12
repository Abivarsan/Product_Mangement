using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProductManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Category", "CreatedAt", "Name", "Price", "Stock", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Electronics", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dell XPS 15 Laptop", 1799.99m, 15, null },
                    { 2, "Electronics", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sony WH-1000XM5 Headphones", 399.99m, 42, null },
                    { 3, "Electronics", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Logitech MX Master 3S Mouse", 99.99m, 80, null },
                    { 4, "Furniture", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ergonomic Mesh Office Chair", 249.50m, 25, null },
                    { 5, "Furniture", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Solid Oak Standing Desk", 649.00m, 12, null },
                    { 6, "Furniture", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Filing Cabinet 3-Drawer", 139.99m, 18, null },
                    { 7, "Books", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "C# 12 and .NET 8 in Action", 49.99m, 95, null },
                    { 8, "Books", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Clean Architecture Patterns", 44.95m, 60, null },
                    { 9, "Apparel", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cotton Crew Neck T-Shirt", 19.99m, 120, null },
                    { 10, "Apparel", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Waterproof Winter Parka", 189.00m, 8, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category",
                table: "Products",
                column: "Category");

            // Embed Requirement 4 Stored Procedures into Code-First Migration
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_GetAveragePricePerCategory
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT 
                        Category,
                        CAST(AVG(Price) AS DECIMAL(18, 2)) AS AveragePrice,
                        COUNT(Id) AS ProductCount
                    FROM Products
                    GROUP BY Category
                    ORDER BY Category ASC;
                END
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_GetHighestStockValueCategory
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT TOP 1
                        Category,
                        CAST(SUM(Price * Stock) AS DECIMAL(18, 2)) AS TotalStockValue,
                        SUM(Stock) AS TotalStockQuantity
                    FROM Products
                    GROUP BY Category
                    ORDER BY TotalStockValue DESC;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetHighestStockValueCategory;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetAveragePricePerCategory;");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
