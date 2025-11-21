using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.Team;
using MatchBy.DTOs.TeamInvite;
using MatchBy.Models;
using MatchBy.Services.Conversations;
using MatchBy.Services.ImageRefresh;
using MatchBy.Services.Notifications;
using MatchBy.Services.S3;
using MatchBy.Services.TeamInvites;
using MatchBy.Services.Teams;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MatchBy.UnitTests.Services.Teams;

public class TeamServiceTests : IDisposable
{
    private readonly Mock<IConversationService> _conversationServiceMock;
    private readonly Mock<ITeamsInvitesService> _teamInvitesServiceMock;
    private readonly Mock<IValidator<CreateTeamDto>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateTeamDto>> _updateValidatorMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IDbContextFactory<ApplicationDbContext>> _dbContextFactoryMock;
    private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;
    private readonly ApplicationDbContext _dbContext;
    private readonly TeamService _teamService;

    public TeamServiceTests()
    {
        var s3ServiceMock = new Mock<IS3Service>();
        _conversationServiceMock = new Mock<IConversationService>();
        _teamInvitesServiceMock = new Mock<ITeamsInvitesService>();
        _createValidatorMock = new Mock<IValidator<CreateTeamDto>>();
        _updateValidatorMock = new Mock<IValidator<UpdateTeamDto>>();
        var imageRefreshServiceMock = new Mock<IImageRefreshService>();
        var notificationServiceMock = new Mock<INotificationService>();
        _dbContextFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();

        // Setup image refresh service to return completed tasks
        imageRefreshServiceMock
            .Setup(s => s.RefreshUserProfileImageAsync(It.IsAny<ApplicationUser>()))
            .Returns(Task.CompletedTask);
        imageRefreshServiceMock
            .Setup(s => s.RefreshTeamImageAsync(It.IsAny<Team>()))
            .Returns(Task.CompletedTask);

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Setup in-memory database with a unique name per test class
        // All contexts created with these options will share the same database
        string databaseName = Guid.NewGuid().ToString();
        _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        // Create a test context for setup and verification
        // This context will share the same in-memory database as contexts created by the factory
        _dbContext = new ApplicationDbContext(_dbContextOptions);

        // Setup the factory to return a new instance each time
        // This prevents the service from disposing the test's context instance
        _dbContextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationDbContext(_dbContextOptions));

        _teamService = new TeamService(
            _dbContextFactoryMock.Object,
            s3ServiceMock.Object,
            _conversationServiceMock.Object,
            _teamInvitesServiceMock.Object,
            _createValidatorMock.Object,
            _updateValidatorMock.Object,
            imageRefreshServiceMock.Object,
            notificationServiceMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    /// <summary>
    /// Helper method to create a fresh DbContext for verification.
    /// This ensures we're querying the database directly rather than using cached entities from the change tracker.
    /// </summary>
    private ApplicationDbContext CreateFreshDbContext()
    {
        return new ApplicationDbContext(_dbContextOptions);
    }

    #region GetTeamByIdAsync Tests

    [Fact]
    public async Task GetTeamByIdAsync_WithPublicTeam_ShouldReturnTeam()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { user },
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Teams.AddAsync(team);
        await _dbContext.SaveChangesAsync();

        _teamInvitesServiceMock
            .Setup(s => s.GetInvitesForTeam(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginationResponse<List<TeamInviteDto>>>.Ok(new PaginationResponse<List<TeamInviteDto>>
            {
                Data = new List<TeamInviteDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 10
            }));

        // Act
        Result<TeamDto> result = await _teamService.GetTeamByIdAsync("team1", "user1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("team1", result.Data.Id);
    }

    #endregion

    #region CreateTeamAsync Tests

    [Fact]
    public async Task CreateTeamAsync_WithValidData_ShouldCreateTeam()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        _userManagerMock.Setup(m => m.Users).Returns(_dbContext.Users);

        var createDto = new CreateTeamDto
        {
            OwnerId = "user1",
            Name = "Test Team",
            Description = "Test Description",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            MembersIds = ["user1"]
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _conversationServiceMock
            .Setup(s => s.CreateConversationAsync(It.IsAny<MatchBy.DTOs.Chat.Conversations.CreateConversationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MatchBy.DTOs.Chat.Conversations.ConversationDto>.Ok(new MatchBy.DTOs.Chat.Conversations.ConversationDto
            {
                Id = "conv1",
                Type = ConversationType.Team,
                CreatorId = "creator1",
                CreatedAtUtc = DateTime.UtcNow,
                MessagesCount = 0
            }));

        // Setup GetInvitesForTeam for GetTeamByIdAsync call
        _teamInvitesServiceMock
            .Setup(s => s.GetInvitesForTeam(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginationResponse<List<TeamInviteDto>>>.Ok(new PaginationResponse<List<TeamInviteDto>>
            {
                Data = new List<TeamInviteDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 10
            }));

        // Act
        Result<TeamDto> result = await _teamService.CreateTeamAsync(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Test Team", result.Data.Name);
    }

    [Fact]
    public async Task CreateTeamAsync_WithInvalidValidation_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateTeamDto
        {
            OwnerId = "user1",
            Name = "",
            Description = "Test Description",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            MembersIds = ["user1"]
        };

        var validationResult = new FluentValidation.Results.ValidationResult(
            [new FluentValidation.Results.ValidationFailure("Name", "Name is required")]);

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        Result<TeamDto> result = await _teamService.CreateTeamAsync(createDto);

        // Assert
        Assert.False(result.Success);
    }

    #endregion

    #region UpdateTeamAsync Tests

    [Fact]
    public async Task UpdateTeamAsync_WithValidData_ShouldUpdateTeam()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        var team = new Team
        {
            Id = "team1",
            Name = "Old Name",
            Description = "Old Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { user },
            CreatedAtUtc = DateTime.UtcNow
        };

        var conversation = new Conversation
        {
            Id = "team1",
            Type = ConversationType.Team,
            Title = "Old Name",
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow
        };

        team.ConversationId = "team1"; // Link team to conversation

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Teams.AddAsync(team);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        var updateDto = new UpdateTeamDto
        {
            Id = "team1",
            OwnerId = "user1",
            Name = "New Name",
            Description = "New Description",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            MembersIds = ["user1"]
        };

        _updateValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _teamInvitesServiceMock
            .Setup(s => s.GetInvitesForTeam(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginationResponse<List<TeamInviteDto>>>.Ok(new PaginationResponse<List<TeamInviteDto>>
            {
                Data = new List<TeamInviteDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 10
            }));

        // Act
        Result<TeamDto> result = await _teamService.UpdateTeamAsync(updateDto);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("New Name", result.Data!.Name);
    }

    [Fact]
    public async Task UpdateTeamAsync_WithNonOwner_ShouldReturnFailure()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { user },
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Teams.AddAsync(team);
        await _dbContext.SaveChangesAsync();

        var updateDto = new UpdateTeamDto
        {
            Id = "team1",
            OwnerId = "user2",
            Name = "New Name",
            Description = "New Description",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            MembersIds = ["user2"]
        };

        _updateValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        Result<TeamDto> result = await _teamService.UpdateTeamAsync(updateDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not the creator", result.ErrorMessages[0]);
    }

    #endregion

    #region DeleteTeamAsync Tests

    [Fact]
    public async Task DeleteTeamAsync_WithNonOwner_ShouldReturnFailure()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { user },
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Teams.AddAsync(team);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<bool> result = await _teamService.DeleteTeamAsync("team1", "user2");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("permission", result.ErrorMessages[0]);
    }

    #endregion

    #region LeaveTeamAsync Tests

    [Fact]
    public async Task LeaveTeamAsync_WithMember_ShouldRemoveMember()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        var member = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com", DisplayName = "User 2" };
        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { owner, member },
            CreatedAtUtc = DateTime.UtcNow
        };

        var conversation = new Conversation
        {
            Id = "team1",
            Type = ConversationType.Team,
            CreatorId = "user1",
            Participants = new List<ApplicationUser> { owner, member },
            CreatedAtUtc = DateTime.UtcNow
        };

        team.ConversationId = "team1"; // Link team to conversation

        await _dbContext.Users.AddRangeAsync(owner, member);
        await _dbContext.Teams.AddAsync(team);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<int> result = await _teamService.LeaveTeamAsync("team1", "user2");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data); // Returns 2 when not soft-deleted
        
        // Use a fresh context to verify the changes were persisted
        await using ApplicationDbContext freshContext = CreateFreshDbContext();
        Team? updatedTeam = await freshContext.Teams.Include(t => t.Members).FirstOrDefaultAsync(t => t.Id == "team1");
        Assert.NotNull(updatedTeam);
        Assert.DoesNotContain(updatedTeam.Members, m => m.Id == "user2");
    }

    #endregion
}


