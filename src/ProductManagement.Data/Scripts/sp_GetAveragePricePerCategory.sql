-- ============================================================================
-- Stored Procedure: sp_GetAveragePricePerCategory
-- Calculation 1: The average price of products in each category
-- ============================================================================
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
END;
GO
