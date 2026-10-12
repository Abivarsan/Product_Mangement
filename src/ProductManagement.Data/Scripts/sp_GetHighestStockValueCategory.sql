-- ============================================================================
-- Stored Procedure: sp_GetHighestStockValueCategory
-- Calculation 2: The highest stock value category (Stock * Price)
-- ============================================================================
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
END;
GO
