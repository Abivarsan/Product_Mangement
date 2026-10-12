using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ProductManagement.Core.Services;

namespace ProductManagement.Tests.Caching;

/// <summary>
/// Unit tests verifying the caching behaviors of MemoryCacheService.
/// - Cached: Cache a generic result up to 5 minutes.
/// - CachedLong: Cache a generic result until cache is removed.
/// </summary>
[TestClass]
public class CacheServiceTests
{
    private IMemoryCache _memoryCache = null!;
    private Mock<ILogger<MemoryCacheService>> _mockLogger = null!;
    private MemoryCacheService _cacheService = null!;

    [TestInitialize]
    public void Setup()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<MemoryCacheService>>();
        _cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _memoryCache.Dispose();
    }

    [TestMethod]
    public void Cached_ExecutesFactoryOnFirstCall_AndReturnsCachedValueOnSecondCall()
    {
        // Arrange
        int executionCount = 0;
        string TestFactory()
        {
            executionCount++;
            return $"Result_{executionCount}";
        }

        // Act
        var firstCall = _cacheService.Cached("test:key:1", TestFactory);
        var secondCall = _cacheService.Cached("test:key:1", TestFactory);

        // Assert
        Assert.AreEqual("Result_1", firstCall);
        Assert.AreEqual("Result_1", secondCall);
        Assert.AreEqual(1, executionCount, "Factory should only be executed once on cache hit.");
    }

    [TestMethod]
    public void CachedLong_PersistsValueUntilExplicitlyRemoved()
    {
        // Arrange
        int factoryCount = 0;
        string TestFactory() => $"LongValue_{++factoryCount}";

        // Act
        var call1 = _cacheService.CachedLong("test:long:1", TestFactory);
        var call2 = _cacheService.CachedLong("test:long:1", TestFactory);

        // Assert
        Assert.AreEqual("LongValue_1", call1);
        Assert.AreEqual("LongValue_1", call2);
        Assert.AreEqual(1, factoryCount);

        // Now remove key explicitly
        _cacheService.Remove("test:long:1");

        var call3 = _cacheService.CachedLong("test:long:1", TestFactory);
        Assert.AreEqual("LongValue_2", call3);
        Assert.AreEqual(2, factoryCount, "After removal, factory must be re-invoked.");
    }

    [TestMethod]
    public void RemoveByPrefix_RemovesAllKeysWithSpecifiedPrefix()
    {
        // Arrange
        _cacheService.Set("reports:summary:1", "data1");
        _cacheService.Set("reports:summary:2", "data2");
        _cacheService.Set("products:list", "products_data");

        // Act
        _cacheService.RemoveByPrefix("reports:");

        // Assert
        Assert.IsNull(_cacheService.Get<string>("reports:summary:1"));
        Assert.IsNull(_cacheService.Get<string>("reports:summary:2"));
        Assert.IsNotNull(_cacheService.Get<string>("products:list"));
    }
}
