using MatchBy.Data;
using MatchBy.Data.Seeders;
using MatchBy.Enums;
using MatchBy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace MatchBy.UnitTests.Data.Seeders;

public class UserSeederTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<ILogger<UserSeeder>> _loggerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;

    public UserSeederTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);

        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            roleStore.Object, null!, null!, null!, null!);

        _loggerMock = new Mock<ILogger<UserSeeder>>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        _serviceProviderMock.Setup(x => x.GetService(typeof(UserManager<ApplicationUser>)))
            .Returns(_userManagerMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(RoleManager<IdentityRole>)))
            .Returns(_roleManagerMock.Object);
    }

    [Fact]
    public async Task SeedAsync_WhenRolesDoNotExist_ShouldCreateRoles()
    {
        // Arrange
        _roleManagerMock.Setup(x => x.Roles).Returns(Array.Empty<IdentityRole>().AsQueryable());
        _userManagerMock.Setup(x => x.Users).Returns(Array.Empty<ApplicationUser>().AsQueryable());
        _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var seeder = new UserSeeder(_loggerMock.Object);

        // Act
        await seeder.SeedAsync(_dbContext, _serviceProviderMock.Object, CancellationToken.None);

        // Assert
        _roleManagerMock.Verify(x => x.CreateAsync(It.Is<IdentityRole>(r => r.Name == Roles.Admin)), Times.Once);
        _roleManagerMock.Verify(x => x.CreateAsync(It.Is<IdentityRole>(r => r.Name == Roles.Member)), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenRolesExist_ShouldNotCreateRoles()
    {
        // Arrange
        IQueryable<IdentityRole> existingRoles = new List<IdentityRole>
        {
            new(Roles.Admin),
            new(Roles.Member)
        }.AsQueryable();

        _roleManagerMock.Setup(x => x.Roles).Returns(existingRoles);
        _userManagerMock.Setup(x => x.Users).Returns(Array.Empty<ApplicationUser>().AsQueryable());
        _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var seeder = new UserSeeder(_loggerMock.Object);

        // Act
        await seeder.SeedAsync(_dbContext, _serviceProviderMock.Object, CancellationToken.None);

        // Assert
        _roleManagerMock.Verify(x => x.CreateAsync(It.IsAny<IdentityRole>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_WhenUsersExist_ShouldNotCreateUsers()
    {
        // Arrange
        IQueryable<ApplicationUser> existingUsers = new List<ApplicationUser>
        {
            new() { UserName = "existing@user.com" }
        }.AsQueryable();

        _roleManagerMock.Setup(x => x.Roles).Returns(Array.Empty<IdentityRole>().AsQueryable());
        _userManagerMock.Setup(x => x.Users).Returns(existingUsers);

        var seeder = new UserSeeder(_loggerMock.Object);

        // Act
        await seeder.SeedAsync(_dbContext, _serviceProviderMock.Object, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_WhenUserCreationFails_ShouldLogError()
    {
        // Arrange
        _roleManagerMock.Setup(x => x.Roles).Returns(Array.Empty<IdentityRole>().AsQueryable());
        _userManagerMock.Setup(x => x.Users).Returns(Array.Empty<ApplicationUser>().AsQueryable());
        _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Creation failed" }));

        var seeder = new UserSeeder(_loggerMock.Object);

        // Act
        await seeder.SeedAsync(_dbContext, _serviceProviderMock.Object, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to create user")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SeedAsync_WhenUserAlreadyExists_ShouldSkipCreation()
    {
        // Arrange
        var existingUser = new ApplicationUser { UserName = "admin@admin.com" };
        _roleManagerMock.Setup(x => x.Roles).Returns(Array.Empty<IdentityRole>().AsQueryable());
        _userManagerMock.Setup(x => x.Users).Returns(new List<ApplicationUser>{existingUser}.AsQueryable());

        var seeder = new UserSeeder(_loggerMock.Object);

        // Act
        await seeder.SeedAsync(_dbContext, _serviceProviderMock.Object, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}

