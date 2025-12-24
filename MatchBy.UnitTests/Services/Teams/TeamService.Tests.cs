using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.Team;
using MatchBy.DTOs.TeamInvite;
using MatchBy.Models;
using MatchBy.Repositories.ChatConversation;
using MatchBy.Repositories.Team;
using MatchBy.Repositories.TeamInvite;
using MatchBy.Repositories.User;
using MatchBy.Services.Conversations;
using MatchBy.Services.ImageRefresh;
using MatchBy.Services.Notifications;
using MatchBy.Services.S3;
using MatchBy.Services.TeamInvites;
using MatchBy.Services.Teams;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MatchBy.UnitTests.Services.Teams;

public class TeamServiceTests : IDisposable
{
    private readonly Mock<IConversationService> _conversationServiceMock;
    private readonly Mock<ITeamsInvitesService> _teamInvitesServiceMock;
    private readonly Mock<IValidator<CreateTeamDto>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateTeamDto>> _updateValidatorMock;
    private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;
    private readonly Mock<ITeamInviteRepository> _teamInviteRepositoryMock;
    private readonly Mock<ITeamRepository> _teamRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly TeamService _teamService;

    public TeamServiceTests()
    {
        var s3ServiceMock = new Mock<IS3Service>();
        _conversationServiceMock = new Mock<IConversationService>();
        _teamInvitesServiceMock = new Mock<ITeamsInvitesService>();
        _createValidatorMock = new Mock<IValidator<CreateTeamDto>>();
        _updateValidatorMock = new Mock<IValidator<UpdateTeamDto>>();
        _teamInviteRepositoryMock = new Mock<ITeamInviteRepository>();
        _teamRepositoryMock = new Mock<ITeamRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        var imageRefreshServiceMock = new Mock<IImageRefreshService>();
        var notificationServiceMock = new Mock<INotificationService>();
        var dbContextFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();

        // Setup image refresh service to return completed tasks
        imageRefreshServiceMock
            .Setup(s => s.RefreshUserProfileImageAsync(It.IsAny<ApplicationUser>()))
            .Returns(Task.CompletedTask);
        imageRefreshServiceMock
            .Setup(s => s.RefreshTeamImageAsync(It.IsAny<Team>()))
            .Returns(Task.CompletedTask);

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
        dbContextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationDbContext(_dbContextOptions));

        _teamService = new TeamService(
            _teamInviteRepositoryMock.Object,
            _teamRepositoryMock.Object,
            _userRepositoryMock.Object,
            _conversationRepositoryMock.Object,
            dbContextFactoryMock.Object,
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


    #region GetTeamByIdAsync Tests

    [Fact]
    public async Task GetTeamByIdAsync_WithPublicTeam_ShouldReturnTeam()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1", EmailConfirmed = true };
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

        _teamRepositoryMock
            .Setup(r => r.GetByIdAsync("team1", "user1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _teamInvitesServiceMock
            .Setup(s => s.GetInvites(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
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
        var user = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1", EmailConfirmed = true };
        var createDto = new CreateTeamDto
        {
            OwnerId = "user1",
            Name = "Test Team",
            Description = "Test Description",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            MembersIds = ["user1"]
        };

        var createdTeam = new Team
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { user },
            CreatedAtUtc = DateTime.UtcNow,
            ConversationId = "team1"
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _teamRepositoryMock
            .Setup(r => r.Add(It.IsAny<Team>(), It.IsAny<ApplicationDbContext>()))
            .Callback<Team, ApplicationDbContext>((t, db) =>
            {
                t.Id = "team1";
            });

        _conversationServiceMock
            .Setup(s => s.CreateConversationAsync(It.IsAny<MatchBy.DTOs.Chat.Conversations.CreateConversationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MatchBy.DTOs.Chat.Conversations.ConversationDto>.Ok(new MatchBy.DTOs.Chat.Conversations.ConversationDto
            {
                Id = "team1",
                Type = ConversationType.Team,
                CreatorId = "user1",
                CreatedAtUtc = DateTime.UtcNow,
                MessagesCount = 0
            }));

        // Setup GetInvitesForTeam for GetTeamByIdAsync call (via service mock above)
        _teamRepositoryMock
            .Setup(r => r.GetByIdAsync("team1", "user1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTeam);

        _teamInvitesServiceMock
            .Setup(s => s.GetInvites(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
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
        _teamRepositoryMock.Verify(r => r.Add(It.IsAny<Team>(), It.IsAny<ApplicationDbContext>()), Times.Once);
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
        var user = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1", EmailConfirmed = true };
        var team = new Team
        {
            Id = "team1",
            Name = "Old Name",
            Description = "Old Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { user },
            CreatedAtUtc = DateTime.UtcNow,
            ConversationId = "team1"
        };

        var conversation = new Conversation
        {
            Id = "team1",
            Type = ConversationType.Team,
            Title = "Old Name",
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow
        };

        var updatedTeam = new Team
        {
            Id = "team1",
            Name = "New Name",
            Description = "New Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { user },
            CreatedAtUtc = DateTime.UtcNow,
            ConversationId = "team1"
        };

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

        _teamRepositoryMock
            .Setup(r => r.GetTeamUserOwnsByIdAsync("team1", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("team1", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        _teamRepositoryMock
            .Setup(r => r.GetByIdAsync("team1", "user1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedTeam);

        _teamInvitesServiceMock
            .Setup(s => s.GetInvites(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
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
        Assert.Equal("New Name", result.Data.Name);
    }

    [Fact]
    public async Task UpdateTeamAsync_WithNonOwner_ShouldReturnFailure()
    {
        // Arrange
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

        _teamRepositoryMock
            .Setup(r => r.GetTeamUserOwnsByIdAsync("team1", "user2", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);

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
        _teamRepositoryMock
            .Setup(r => r.GetTeamUserOwnsByIdAsync("team1", "user2", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);

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
        var owner = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1", EmailConfirmed = true };
        var member = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com", DisplayName = "User 2", EmailConfirmed = true };
        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { owner, member },
            CreatedAtUtc = DateTime.UtcNow,
            ConversationId = "team1"
        };

        var conversation = new Conversation
        {
            Id = "team1",
            Type = ConversationType.Team,
            CreatorId = "user1",
            Participants = new List<ApplicationUser> { owner, member },
            CreatedAtUtc = DateTime.UtcNow
        };

        _teamRepositoryMock
            .Setup(r => r.GetTeamUserParticipatesByIdAsync("team1", "user2", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("team1", "user2", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        // Act
        Result<int> result = await _teamService.LeaveTeamAsync("team1", "user2");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data); // Returns 2 when not soft-deleted
        Assert.DoesNotContain(team.Members, m => m.Id == "user2");
        Assert.DoesNotContain(conversation.Participants, p => p.Id == "user2");
    }

    #endregion
}


