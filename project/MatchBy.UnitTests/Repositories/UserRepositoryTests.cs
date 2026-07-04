using MatchBy.Data;
using MatchBy.Models;
using MatchBy.Repositories.User;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.UnitTests.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _repository = new UserRepository();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnUser()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "test@test.com",
            EmailConfirmed = true
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        ApplicationUser? result = await _repository.GetByIdAsync("user1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user1", result.Id);
        Assert.Equal("testuser", result.UserName);
        Assert.Equal("Test User", result.DisplayName);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "test@test.com",
            EmailConfirmed = true
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        ApplicationUser? result = await _repository.GetByIdAsync("nonexistent", _dbContext);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldIncludeJoinedMatches()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "test@test.com",
            EmailConfirmed = true
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        ApplicationUser? result = await _repository.GetByIdAsync("user1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.JoinedMatches);
    }

    #endregion

    #region GetUsers Tests

    [Fact]
    public async Task GetUsers_WithNoSearch_ShouldReturnAllUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true },
            new() { Id = "2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true }
        };

        await _dbContext.Users.AddRangeAsync(users);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<ApplicationUser>> result = await _repository.GetUsers(_dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetUsers_WithSearchMatchingUsername_ShouldReturnMatchingUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "john_doe", DisplayName = "John Doe", Email = "john@test.com", EmailConfirmed = true },
            new() { Id = "2", UserName = "jane_smith", DisplayName = "Jane Smith", Email = "jane@test.com", EmailConfirmed = true },
            new() { Id = "3", UserName = "bob_jones", DisplayName = "Bob Jones", Email = "bob@test.com", EmailConfirmed = true }
        };

        await _dbContext.Users.AddRangeAsync(users);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<ApplicationUser>> result = await _repository.GetUsers(_dbContext, search: "john");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal("john_doe", result.Data[0].UserName);
    }

    [Fact]
    public async Task GetUsers_WithSearchMatchingDisplayName_ShouldReturnMatchingUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "user1", DisplayName = "Alice Wonder", Email = "alice@test.com", EmailConfirmed = true },
            new() { Id = "2", UserName = "user2", DisplayName = "Bob Builder", Email = "bob@test.com", EmailConfirmed = true }
        };

        await _dbContext.Users.AddRangeAsync(users);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<ApplicationUser>> result = await _repository.GetUsers(_dbContext, search: "Alice");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal("Alice Wonder", result.Data[0].DisplayName);
    }

    [Fact]
    public async Task GetUsers_WithCaseInsensitiveSearch_ShouldReturnMatches()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "1",
            UserName = "TestUser",
            DisplayName = "Test User",
            Email = "test@test.com",
            EmailConfirmed = true
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<ApplicationUser>> result = await _repository.GetUsers(_dbContext, search: "testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
    }

    [Fact]
    public async Task GetUsers_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var users = new List<ApplicationUser>();
        for (int i = 1; i <= 10; i++)
        {
            users.Add(new ApplicationUser
            {
                Id = $"user{i}",
                UserName = $"user{i}",
                DisplayName = $"User {i}",
                Email = $"user{i}@test.com",
                EmailConfirmed = true
            });
        }

        await _dbContext.Users.AddRangeAsync(users);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<ApplicationUser>> result = await _repository.GetUsers(_dbContext, page: 2, pageSize: 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Data.Count);
        Assert.Equal(10, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(3, result.PageSize);
    }

    [Fact]
    public async Task GetUsers_ShouldOrderByDisplayName()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "user1", DisplayName = "Charlie", Email = "c@test.com", EmailConfirmed = true },
            new() { Id = "2", UserName = "user2", DisplayName = "Alice", Email = "a@test.com", EmailConfirmed = true },
            new() { Id = "3", UserName = "user3", DisplayName = "Bob", Email = "b@test.com", EmailConfirmed = true }
        };

        await _dbContext.Users.AddRangeAsync(users);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<ApplicationUser>> result = await _repository.GetUsers(_dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Data.Count);
        Assert.Equal("Alice", result.Data[0].DisplayName);
        Assert.Equal("Bob", result.Data[1].DisplayName);
        Assert.Equal("Charlie", result.Data[2].DisplayName);
    }

    #endregion

    #region GetUsersByIdsAsync Tests

    [Fact]
    public async Task GetUsersByIdsAsync_WithValidIds_ShouldReturnMatchingUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true },
            new() { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true },
            new() { Id = "user3", UserName = "user3", DisplayName = "User 3", Email = "user3@test.com", EmailConfirmed = true }
        };

        await _dbContext.Users.AddRangeAsync(users);
        await _dbContext.SaveChangesAsync();

        // Act
        List<ApplicationUser> result = await _repository.GetUsersByIdsAsync(new List<string> { "user1", "user2" }, _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Id == "user1");
        Assert.Contains(result, u => u.Id == "user2");
    }

    [Fact]
    public async Task GetUsersByIdsAsync_WithNonExistentIds_ShouldReturnEmptyList()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "user1",
            DisplayName = "User 1",
            Email = "user1@test.com",
            EmailConfirmed = true
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        List<ApplicationUser> result = await _repository.GetUsersByIdsAsync(new List<string> { "nonexistent1", "nonexistent2" }, _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUsersByIdsAsync_WithEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "user1",
            DisplayName = "User 1",
            Email = "user1@test.com",
            EmailConfirmed = true
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        List<ApplicationUser> result = await _repository.GetUsersByIdsAsync(new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUsersByIdsAsync_ShouldIncludeJoinedMatches()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "user1",
            DisplayName = "User 1",
            Email = "user1@test.com",
            EmailConfirmed = true
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        List<ApplicationUser> result = await _repository.GetUsersByIdsAsync(new List<string> { "user1" }, _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.NotNull(result[0].JoinedMatches);
    }

    #endregion
}

