using MatchBy.Data;
using MatchBy.Data.Seeders;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MatchBy.UnitTests.Data.Seeders;

public class ApplicationSeederTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;

    public ApplicationSeederTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SeedAsync_WhenCalled_ShouldCallAllSeeders()
    {
        // Arrange
        var seeder1 = new Mock<ISeeder>();
        var seeder2 = new Mock<ISeeder>();
        var seeder3 = new Mock<ISeeder>();
        
        var seeders = new List<ISeeder> { seeder1.Object, seeder2.Object, seeder3.Object };
        var applicationSeeder = new ApplicationSeeder(seeders);
        var serviceProvider = new Mock<IServiceProvider>();

        // Act
        await applicationSeeder.SeedAsync(_dbContext, serviceProvider.Object, CancellationToken.None);

        // Assert
        seeder1.Verify(x => x.SeedAsync(_dbContext, serviceProvider.Object, CancellationToken.None), Times.Once);
        seeder2.Verify(x => x.SeedAsync(_dbContext, serviceProvider.Object, CancellationToken.None), Times.Once);
        seeder3.Verify(x => x.SeedAsync(_dbContext, serviceProvider.Object, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenNoSeeders_ShouldNotThrow()
    {
        // Arrange
        var seeders = new List<ISeeder>();
        var applicationSeeder = new ApplicationSeeder(seeders);
        var serviceProvider = new Mock<IServiceProvider>();

        // Act & Assert
        await applicationSeeder.SeedAsync(_dbContext, serviceProvider.Object, CancellationToken.None);
        Assert.True(true);
    }

    [Fact]
    public async Task SeedAsync_WhenSeederThrows_ShouldPropagateException()
    {
        // Arrange
        var seeder1 = new Mock<ISeeder>();
        seeder1.Setup(x => x.SeedAsync(It.IsAny<ApplicationDbContext>(), It.IsAny<IServiceProvider>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Seeder error"));
        
        var seeders = new List<ISeeder> { seeder1.Object };
        var applicationSeeder = new ApplicationSeeder(seeders);
        var serviceProvider = new Mock<IServiceProvider>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            applicationSeeder.SeedAsync(_dbContext, serviceProvider.Object, CancellationToken.None));
    }

    [Fact]
    public async Task SeedAsync_WhenCalledWithCancellationToken_ShouldPassTokenToSeeders()
    {
        // Arrange
        var cancellationToken = new CancellationToken(true);
        var seeder = new Mock<ISeeder>();
        var seeders = new List<ISeeder> { seeder.Object };
        var applicationSeeder = new ApplicationSeeder(seeders);
        var serviceProvider = new Mock<IServiceProvider>();

        // Act
        await applicationSeeder.SeedAsync(_dbContext, serviceProvider.Object, cancellationToken);

        // Assert
        seeder.Verify(x => x.SeedAsync(_dbContext, serviceProvider.Object, cancellationToken), Times.Once);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}

