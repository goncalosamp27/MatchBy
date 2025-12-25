using Amazon.S3;
using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.DTOs.Team;
using MatchBy.DTOs.TeamInvite;
using MatchBy.Enums;
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
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MatchBy.UnitTests.Services.Teams;

public class TeamServiceTests : IDisposable
{
    private readonly Mock<IConversationService> _conversationServiceMock;
    private readonly Mock<ITeamsInvitesService> _teamInvitesServiceMock;
    private readonly Mock<IValidator<CreateTeamDto>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateTeamDto>> _updateValidatorMock;
    private readonly Mock<IS3Service> _s3ServiceMock;
    private readonly Mock<ITeamInviteRepository> _teamInviteRepositoryMock;
    private readonly Mock<ITeamRepository> _teamRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly TeamService _teamService;

    public TeamServiceTests()
    {
        _s3ServiceMock = new Mock<IS3Service>();
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
        DbContextOptions<ApplicationDbContext> dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        // Create a test context for setup and verification
        // This context will share the same in-memory database as contexts created by the factory
        _dbContext = new ApplicationDbContext(dbContextOptions);

        // Setup the factory to return a new instance each time
        // This prevents the service from disposing the test's context instance
        dbContextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationDbContext(dbContextOptions));

        _teamService = new TeamService(
            _teamInviteRepositoryMock.Object,
            _teamRepositoryMock.Object,
            _userRepositoryMock.Object,
            _conversationRepositoryMock.Object,
            dbContextFactoryMock.Object,
            _s3ServiceMock.Object,
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


    #region GetTeamsAsync Tests

    [Fact]
    public async Task GetTeamsAsync_WithValidParameters_ShouldReturnTeams()
    {
        // Arrange
        var teamQueryParametersDto = new TeamQueryParametersDto
        {
            UserId = "user1",
            Page = 1,
            PageSize = 10,
            Query = "",
            SortBy = SortBy.Name,
            OrderBy = OrderBy.Ascending,
            Privacy = Privacy.All
        };

        var invitedTeamIds = new List<TeamInvite>
        {
            new TeamInvite { TeamId = "team1", ReceiverId = "user1", Status = InviteStatus.Pending }
        };

        var teams = new List<Team>
        {
            new Team
            {
                Id = "team1",
                Name = "Test Team",
                Description = "Test Description",
                OwnerId = "owner1",
                Privacy = TeamPrivacy.Public,
                MaxMembers = 10,
                Members = new List<ApplicationUser>
                {
                    new() { Id = "owner1", UserName = "owner", DisplayName = "Owner" },
                    new() { Id = "member1", UserName = "member", DisplayName = "Member" }
                },
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        var teamsResponse = new PaginationResponse<List<Team>>
        {
            Data = teams,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _teamInviteRepositoryMock
            .Setup(r => r.GetInvites("user1", It.IsAny<ApplicationDbContext>(), 1, int.MaxValue - 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginationResponse<List<TeamInvite>>
            {
                Data = invitedTeamIds,
                TotalCount = 1,
                Page = 1,
                PageSize = int.MaxValue - 1
            });

        _teamRepositoryMock
            .Setup(r => r.GetTeamsAsync(teamQueryParametersDto, It.IsAny<List<string>>(), It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(teamsResponse);

        // Act
        Result<PaginationResponse<List<TeamDto>>> result = await _teamService.GetTeamsAsync(teamQueryParametersDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
        Assert.Equal("team1", result.Data.Data[0].Id);
    }

    #endregion

    #region GetAvailableTeamsAsync Tests

    [Fact]
    public async Task GetAvailableTeamsAsync_WithValidParameters_ShouldReturnAvailableTeams()
    {
        // Arrange
        var teamQueryParametersDto = new TeamQueryParametersDto
        {
            UserId = "user1",
            Page = 1,
            PageSize = 10,
            Query = "",
            SortBy = SortBy.Name,
            OrderBy = OrderBy.Ascending,
            Privacy = Privacy.All
        };

        var invitedTeamIds = new List<TeamInvite>
        {
            new TeamInvite { TeamId = "team1", ReceiverId = "user1", Status = InviteStatus.Pending }
        };

        var availableTeams = new List<Team>
        {
            new Team
            {
                Id = "team1",
                Name = "Available Team",
                Description = "Available Description",
                OwnerId = "owner1",
                Privacy = TeamPrivacy.Public,
                MaxMembers = 10,
                Members = new List<ApplicationUser>
                {
                    new() { Id = "owner1", UserName = "owner", DisplayName = "Owner" }
                },
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        var teamsResponse = new PaginationResponse<List<Team>>
        {
            Data = availableTeams,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _teamInviteRepositoryMock
            .Setup(r => r.GetInvites("user1", It.IsAny<ApplicationDbContext>(), 1, int.MaxValue - 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginationResponse<List<TeamInvite>>
            {
                Data = invitedTeamIds,
                TotalCount = 1,
                Page = 1,
                PageSize = int.MaxValue - 1
            });

        _teamRepositoryMock
            .Setup(r => r.GetAvailableTeamsAsync(teamQueryParametersDto, It.IsAny<List<string>>(), It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(teamsResponse);

        // Act
        Result<PaginationResponse<List<TeamDto>>> result = await _teamService.GetAvailableTeamsAsync(teamQueryParametersDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
        Assert.Equal("Available Team", result.Data.Data[0].Name);
    }

    #endregion

    #region GetTeamsUserOwnAsync Tests

    [Fact]
    public async Task GetTeamsUserOwnAsync_WithValidUserId_ShouldReturnOwnedTeams()
    {
        // Arrange
        string userId = "user1";
        int page = 1;
        int pageSize = 10;
        string query = "test";

        var ownedTeams = new List<Team>
        {
            new Team
            {
                Id = "team1",
                Name = "My Team",
                Description = "My Description",
                OwnerId = userId,
                Privacy = TeamPrivacy.Public,
                MaxMembers = 10,
                Members = new List<ApplicationUser>
                {
                    new() { Id = userId, UserName = "user", DisplayName = "User" }
                },
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        var teamsResponse = new PaginationResponse<List<Team>>
        {
            Data = ownedTeams,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _teamRepositoryMock
            .Setup(r => r.GetTeamsUserOwnAsync(userId, page, pageSize, query, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(teamsResponse);

        // Act
        Result<PaginationResponse<List<TeamDto>>> result = await _teamService.GetTeamsUserOwnAsync(userId, page, pageSize, query);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
        Assert.Equal("My Team", result.Data.Data[0].Name);
    }

    [Fact]
    public async Task GetTeamsUserOwnAsync_WithEmptyUserId_ShouldReturnFailure()
    {
        // Act
        Result<PaginationResponse<List<TeamDto>>> result = await _teamService.GetTeamsUserOwnAsync("", 1, 10, "");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("User ID cannot be null or empty", result.ErrorMessages[0]);
    }

    #endregion

    #region GetTeamsUserParticipateAsync Tests

    [Fact]
    public async Task GetTeamsUserParticipateAsync_WithValidUserId_ShouldReturnParticipatingTeams()
    {
        // Arrange
        string userId = "user1";
        int page = 1;
        int pageSize = 10;
        string query = "test";

        var participatingTeams = new List<Team>
        {
            new Team
            {
                Id = "team1",
                Name = "Participating Team",
                Description = "Participating Description",
                OwnerId = "owner1",
                Privacy = TeamPrivacy.Public,
                MaxMembers = 10,
                Members = new List<ApplicationUser>
                {
                    new() { Id = "owner1", UserName = "owner", DisplayName = "Owner" },
                    new() { Id = userId, UserName = "user", DisplayName = "User" }
                },
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        var teamsResponse = new PaginationResponse<List<Team>>
        {
            Data = participatingTeams,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _teamRepositoryMock
            .Setup(r => r.GetTeamsUserOwnAsync(userId, page, pageSize, query, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(teamsResponse);

        // Act
        Result<PaginationResponse<List<TeamDto>>> result = await _teamService.GetTeamsUserParticipateAsync(userId, page, pageSize, query);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
        Assert.Equal("Participating Team", result.Data.Data[0].Name);
    }

    #endregion

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

    [Fact]
    public async Task GetTeamByIdAsync_WithPrivateTeamAndInvite_ShouldReturnTeam()
    {
        // Arrange
        var team = new Team
        {
            Id = "team1",
            Name = "Private Team",
            Description = "Private Description",
            OwnerId = "owner1",
            Privacy = TeamPrivacy.Private,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { new() { Id = "owner1", UserName = "owner", DisplayName = "Owner" } },
            CreatedAtUtc = DateTime.UtcNow
        };

        var invites = new List<TeamInviteDto>
        {
            new()
            {
                Id = "invite1",
                TeamId = "team1",
                ReceiverId = "user1",
                SenderId = "owner1",
                Status = InviteStatus.Pending,
                Content = "Join my team",
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        _teamRepositoryMock
            .Setup(r => r.GetByIdAsync("team1", "user1", true, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _teamInvitesServiceMock
            .Setup(s => s.GetInvites("team1", 1, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginationResponse<List<TeamInviteDto>>>.Ok(new PaginationResponse<List<TeamInviteDto>>
            {
                Data = invites,
                TotalCount = 1,
                Page = 1,
                PageSize = int.MaxValue
            }));

        // Act
        Result<TeamDto> result = await _teamService.GetTeamByIdAsync("team1", "user1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("team1", result.Data.Id);
    }

    [Fact]
    public async Task GetTeamByIdAsync_WithPrivateTeamNoAccess_ShouldReturnFailure()
    {
        // Arrange
        _teamInvitesServiceMock
            .Setup(s => s.GetInvites("team1", 1, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginationResponse<List<TeamInviteDto>>>.Ok(new PaginationResponse<List<TeamInviteDto>>
            {
                Data = new List<TeamInviteDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = int.MaxValue
            }));

        // Act
        Result<TeamDto> result = await _teamService.GetTeamByIdAsync("team1", "user1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Team not found or access denied", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task GetTeamByIdAsync_WithNonExistentTeam_ShouldReturnFailure()
    {
        // Arrange
        _teamRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", "user1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);

        _teamInvitesServiceMock
            .Setup(s => s.GetInvites("nonexistent", 1, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginationResponse<List<TeamInviteDto>>>.Ok(new PaginationResponse<List<TeamInviteDto>>
            {
                Data = new List<TeamInviteDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = int.MaxValue
            }));

        // Act
        Result<TeamDto> result = await _teamService.GetTeamByIdAsync("nonexistent", "user1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Team not found or access denied", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task GetTeamByIdAsync_WithFailedInviteRetrieval_ShouldReturnFailure()
    {
        // Arrange
        _teamInvitesServiceMock
            .Setup(s => s.GetInvites("team1", 1, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginationResponse<List<TeamInviteDto>>>.Fail("Failed to retrieve invites"));

        // Act
        Result<TeamDto> result = await _teamService.GetTeamByIdAsync("team1", "user1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Failed to retrieve team invites", result.ErrorMessages[0]);
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
            .Setup(s => s.CreateConversationAsync(It.IsAny<CreateConversationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Ok(new ConversationDto
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

    [Fact]
    public async Task CreateTeamAsync_WithNonExistentOwner_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateTeamDto
        {
            OwnerId = "nonexistent",
            Name = "Test Team",
            Description = "Test Description",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            MembersIds = ["nonexistent"]
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        Result<TeamDto> result = await _teamService.CreateTeamAsync(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("User not found", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task CreateTeamAsync_WithFailedConversationCreation_ShouldReturnFailure()
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

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _conversationServiceMock
            .Setup(s => s.CreateConversationAsync(It.IsAny<CreateConversationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Fail("Failed to create conversation"));

        // Act
        Result<TeamDto> result = await _teamService.CreateTeamAsync(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Failed to create associated conversation", result.ErrorMessages[0]);
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

    [Fact]
    public async Task UpdateTeamAsync_WithInvalidValidation_ShouldReturnFailure()
    {
        // Arrange
        var updateDto = new UpdateTeamDto
        {
            Id = "team1",
            OwnerId = "user1",
            Name = "",
            Description = "New Description",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            MembersIds = ["user1"]
        };

        var validationResult = new FluentValidation.Results.ValidationResult(
            [new FluentValidation.Results.ValidationFailure("Name", "Name is required")]);

        _updateValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        Result<TeamDto> result = await _teamService.UpdateTeamAsync(updateDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Name is required", result.ErrorMessages);
    }

    [Fact]
    public async Task UpdateTeamAsync_WithMissingConversation_ShouldReturnFailure()
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
            .ReturnsAsync((Conversation?)null);

        // Act
        Result<TeamDto> result = await _teamService.UpdateTeamAsync(updateDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Associated conversation not found", result.ErrorMessages[0]);
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

    [Fact]
    public async Task DeleteTeamAsync_WithMissingConversation_ShouldReturnFailure()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1", EmailConfirmed = true };
        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { owner },
            CreatedAtUtc = DateTime.UtcNow,
            ConversationId = "team1"
        };

        _teamRepositoryMock
            .Setup(r => r.GetTeamUserOwnsByIdAsync("team1", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _teamRepositoryMock
            .Setup(r => r.GetByIdAsync("team1", "user1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        // Act
        Result<bool> result = await _teamService.DeleteTeamAsync("team1", "user1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Associated conversation not found", result.ErrorMessages[0]);
    }

    #endregion

    #region DeleteTeamImageAsync Tests


    [Fact]
    public async Task DeleteTeamImageAsync_WithNoImage_ShouldReturnFailure()
    {
        // Arrange
        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<ApplicationUser>(),
            CreatedAtUtc = DateTime.UtcNow,
            Image = null
        };

        _teamRepositoryMock
            .Setup(r => r.GetTeamUserOwnsByIdAsync("team1", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        // Act
        Result<bool> result = await _teamService.DeleteTeamImageAsync("team1", "user1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Team does not have an image to delete", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task DeleteTeamImageAsync_WithNonOwner_ShouldReturnFailure()
    {
        // Arrange
        _teamRepositoryMock
            .Setup(r => r.GetTeamUserOwnsByIdAsync("team1", "user2", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);

        // Act
        Result<bool> result = await _teamService.DeleteTeamImageAsync("team1", "user2");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Team not found or user is not the owner", result.ErrorMessages[0]);
    }

    #endregion

    #region LeaveTeamAsync Tests

    [Fact]
    public async Task LeaveTeamAsync_WithOwner_ShouldSoftDeleteTeam()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1", EmailConfirmed = true };

        var conversation = new Conversation
        {
            Id = "team1",
            Type = ConversationType.Team,
            CreatorId = "user1",
            Participants = new List<ApplicationUser> { owner },
            Messages = new List<ChatMessage>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { owner },
            CreatedAtUtc = DateTime.UtcNow,
            ConversationId = "team1",
            Conversation = conversation
        };

        _teamRepositoryMock
            .Setup(r => r.GetTeamUserParticipatesByIdAsync("team1", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        // Mock the DeleteTeamAsync method call
        _teamRepositoryMock
            .Setup(r => r.GetTeamUserOwnsByIdAsync("team1", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _teamRepositoryMock
            .Setup(r => r.GetByIdAsync("team1", "user1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _teamInviteRepositoryMock
            .Setup(r => r.GetInvites("team1", It.IsAny<ApplicationDbContext>(), 1, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginationResponse<List<TeamInvite>>
            {
                Data = new List<TeamInvite>(),
                TotalCount = 0,
                Page = 1,
                PageSize = int.MaxValue
            });

        // Act
        Result<int> result = await _teamService.LeaveTeamAsync("team1", "user1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.Data); // Returns 1 when soft-deleted
    }

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

    [Fact]
    public async Task LeaveTeamAsync_WithNonParticipant_ShouldReturnFailure()
    {
        // Arrange
        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { new() { Id = "user1", UserName = "user1", DisplayName = "User 1" } },
            CreatedAtUtc = DateTime.UtcNow,
            ConversationId = "team1"
        };

        _teamRepositoryMock
            .Setup(r => r.GetTeamUserParticipatesByIdAsync("team1", "user2", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        // Act
        Result<int> result = await _teamService.LeaveTeamAsync("team1", "user2");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("User is not a participant of the conversation", result.ErrorMessages[0]);
    }

    #endregion

    #region JoinTeamAsync Tests

    [Fact]
    public async Task JoinTeamAsync_WithPublicTeam_ShouldAddUser()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1", EmailConfirmed = true };
        var newUser = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com", DisplayName = "User 2", EmailConfirmed = true };
        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { owner },
            CreatedAtUtc = DateTime.UtcNow,
            ConversationId = "team1",
            Conversation = new Conversation
            {
                Id = "team1",
                Type = ConversationType.Team,
                CreatorId = "user1",
                Participants = new List<ApplicationUser> { owner },
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        _teamInvitesServiceMock
            .Setup(s => s.GetInvites("team1", 1, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginationResponse<List<TeamInviteDto>>>.Ok(new PaginationResponse<List<TeamInviteDto>>
            {
                Data = new List<TeamInviteDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = int.MaxValue
            }));

        _teamRepositoryMock
            .Setup(r => r.GetByIdAsync("team1", "user2", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("user2", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newUser);

        // Act
        Result<bool> result = await _teamService.JoinTeamAsync("team1", "user2");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
        Assert.Contains(team.Members, m => m.Id == "user2");
        Assert.Contains(team.Conversation!.Participants, p => p.Id == "user2");
    }

    [Fact]
    public async Task JoinTeamAsync_WithPrivateTeamAndInvite_ShouldAcceptInvite()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1", EmailConfirmed = true };
        var newUser = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com", DisplayName = "User 2", EmailConfirmed = true };
        var team = new Team
        {
            Id = "team1",
            Name = "Private Team",
            Description = "Private Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Private,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { owner },
            CreatedAtUtc = DateTime.UtcNow,
            ConversationId = "team1",
            Conversation = new Conversation
            {
                Id = "team1",
                Type = ConversationType.Team,
                CreatorId = "user1",
                Participants = new List<ApplicationUser> { owner },
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        var invites = new List<TeamInviteDto>
        {
            new TeamInviteDto
            {
                Id = "invite1",
                TeamId = "team1",
                ReceiverId = "user2",
                SenderId = "user1",
                Status = InviteStatus.Pending,
                Content = "Join my team",
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        _teamInvitesServiceMock
            .Setup(s => s.GetInvites("team1", 1, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginationResponse<List<TeamInviteDto>>>.Ok(new PaginationResponse<List<TeamInviteDto>>
            {
                Data = invites,
                TotalCount = 1,
                Page = 1,
                PageSize = int.MaxValue
            }));

        _teamRepositoryMock
            .Setup(r => r.GetByIdAsync("team1", "user2", true, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("user2", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newUser);

        _teamInvitesServiceMock
            .Setup(s => s.AcceptInvite("invite1", "user2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TeamInviteDto>.Ok(invites[0]));

        // Act
        Result<bool> result = await _teamService.JoinTeamAsync("team1", "user2");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
        _teamInvitesServiceMock.Verify(s => s.AcceptInvite("invite1", "user2", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task JoinTeamAsync_WithAlreadyMember_ShouldReturnFailure()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1", EmailConfirmed = true };
        var existingUser = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com", DisplayName = "User 2", EmailConfirmed = true };
        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { owner, existingUser },
            CreatedAtUtc = DateTime.UtcNow,
            ConversationId = "team1"
        };

        _teamInvitesServiceMock
            .Setup(s => s.GetInvites("team1", 1, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginationResponse<List<TeamInviteDto>>>.Ok(new PaginationResponse<List<TeamInviteDto>>
            {
                Data = new List<TeamInviteDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = int.MaxValue
            }));

        _teamRepositoryMock
            .Setup(r => r.GetByIdAsync("team1", "user2", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("user2", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        Result<bool> result = await _teamService.JoinTeamAsync("team1", "user2");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("User is already a member of this team", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task JoinTeamAsync_WithPrivateTeamNoInvite_ShouldReturnFailure()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1", EmailConfirmed = true };
        var newUser = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com", DisplayName = "User 2", EmailConfirmed = true };
        var team = new Team
        {
            Id = "team1",
            Name = "Private Team",
            Description = "Private Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Private,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { owner },
            CreatedAtUtc = DateTime.UtcNow,
            ConversationId = "team1"
        };

        _teamInvitesServiceMock
            .Setup(s => s.GetInvites("team1", 1, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginationResponse<List<TeamInviteDto>>>.Ok(new PaginationResponse<List<TeamInviteDto>>
            {
                Data = new List<TeamInviteDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = int.MaxValue
            }));

        _teamRepositoryMock
            .Setup(r => r.GetByIdAsync("team1", "user2", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("user2", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newUser);

        // Act
        Result<bool> result = await _teamService.JoinTeamAsync("team1", "user2");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invite is required to join this private team", result.ErrorMessages[0]);
    }

    #region UpdateTeamImageAsync Tests (through CreateTeamAsync and UpdateTeamAsync)

    [Fact]
    public async Task CreateTeamAsync_WithImageUploadFailure_ShouldReturnFailure()
    {
        // Arrange
        var mockFile = new Mock<IBrowserFile>();
        mockFile.Setup(f => f.Name).Returns("test-image.jpg");
        mockFile.Setup(f => f.Size).Returns(1024);
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

        var createDto = new CreateTeamDto
        {
            OwnerId = "user1",
            Name = "Test Team",
            Description = "Test Description",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            MembersIds = ["user1"],
            File = mockFile.Object
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Setup S3 service to fail on upload
        _s3ServiceMock.Setup(s => s.UploadBrowserFileAsync(mockFile.Object, "teams/team1/image"))
            .ReturnsAsync(Result<string>.Fail("Upload failed"));

        // Act
        Result<TeamDto> result = await _teamService.CreateTeamAsync(createDto);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateTeamAsync_WithNewImage_ShouldReplaceExistingImage()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1", EmailConfirmed = true };
        var mockFile = new Mock<IBrowserFile>();
        mockFile.Setup(f => f.Name).Returns("new-image.jpg");
        mockFile.Setup(f => f.Size).Returns(2048);
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

        var existingTeam = new Team
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { owner },
            Image = new FileStore("https://old-url.com/old-key.jpg", DateTime.UtcNow.AddMinutes(25), "old-key.jpg", FileCategory.TeamImage, FileType.Image, DateTime.UtcNow),
            ConversationId = "team1"
        };

        var updateDto = new UpdateTeamDto
        {
            Id = "team1",
            OwnerId = "user1",
            Name = "Updated Team",
            Description = "Updated Description",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 15,
            MembersIds = ["user1"],
            File = mockFile.Object
        };

        _updateValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _teamRepositoryMock.Setup(r => r.GetByIdAsync("team1", "user1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(existingTeam);

        // Setup S3 service for image operations
        _s3ServiceMock.Setup(s => s.UploadBrowserFileAsync(mockFile.Object, "teams/team1/image"))
            .ReturnsAsync(Result<string>.Ok("new-image-key.jpg"));
        _s3ServiceMock.Setup(s => s.GetPresignedUrlAsync("teams/team1/image/new-image-key.jpg", HttpVerb.GET))
            .ReturnsAsync(Result<string>.Ok("https://presigned-url.com/new-image-key.jpg"));

        // Act
        Result<TeamDto> result = await _teamService.UpdateTeamAsync(updateDto);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateTeamAsync_WithImagePresignedUrlFailure_ShouldReturnFailure()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1", EmailConfirmed = true };
        var mockFile = new Mock<IBrowserFile>();
        mockFile.Setup(f => f.Name).Returns("new-image.jpg");
        mockFile.Setup(f => f.Size).Returns(2048);
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

        var existingTeam = new Team
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { owner },
            ConversationId = "team1"
        };

        var updateDto = new UpdateTeamDto
        {
            Id = "team1",
            OwnerId = "user1",
            Name = "Updated Team",
            Description = "Updated Description",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 15,
            MembersIds = ["user1"],
            File = mockFile.Object
        };

        _updateValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _teamRepositoryMock.Setup(r => r.GetByIdAsync("team1", "user1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(existingTeam);

        // Setup S3 service - upload succeeds but presigned URL fails
        _s3ServiceMock.Setup(s => s.UploadBrowserFileAsync(mockFile.Object, "teams/team1/image"))
            .ReturnsAsync(Result<string>.Ok("new-image-key.jpg"));
        _s3ServiceMock.Setup(s => s.GetPresignedUrlAsync("teams/team1/image/new-image-key.jpg", HttpVerb.GET))
            .ReturnsAsync(Result<string>.Fail("Presigned URL generation failed"));

        // Act
        Result<TeamDto> result = await _teamService.UpdateTeamAsync(updateDto);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateTeamAsync_WithSameImageKey_ShouldNotDeleteImage()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1", EmailConfirmed = true };
        var mockFile = new Mock<IBrowserFile>();
        mockFile.Setup(f => f.Name).Returns("same-image.jpg");
        mockFile.Setup(f => f.Size).Returns(1024);
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

        var existingTeam = new Team
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<ApplicationUser> { owner },
            Image = new FileStore("https://old-url.com/same-key.jpg", DateTime.UtcNow.AddMinutes(25), "same-key.jpg", FileCategory.TeamImage, FileType.Image, DateTime.UtcNow),
            ConversationId = "team1"
        };

        var updateDto = new UpdateTeamDto
        {
            Id = "team1",
            OwnerId = "user1",
            Name = "Updated Team",
            Description = "Updated Description",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 15,
            MembersIds = ["user1"],
            File = mockFile.Object
        };

        _updateValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _teamRepositoryMock.Setup(r => r.GetByIdAsync("team1", "user1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(existingTeam);

        // Setup S3 service - upload returns the same key
        _s3ServiceMock.Setup(s => s.UploadBrowserFileAsync(mockFile.Object, "teams/team1/image"))
            .ReturnsAsync(Result<string>.Ok("same-key.jpg")); // Same key as existing
        _s3ServiceMock.Setup(s => s.GetPresignedUrlAsync("teams/team1/image/same-key.jpg", HttpVerb.GET))
            .ReturnsAsync(Result<string>.Ok("https://presigned-url.com/same-key.jpg"));

        // Act
        Result<TeamDto> result = await _teamService.UpdateTeamAsync(updateDto);

        // Assert
        Assert.False(result.Success);
    }

    #endregion

    #endregion
}


