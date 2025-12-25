using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.DTOs.Match;
using MatchBy.DTOs.MatchInvite;
using MatchBy.DTOs.Notification;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Repositories.ChatConversation;
using MatchBy.Repositories.Friend;
using MatchBy.Repositories.Match;
using MatchBy.Repositories.User;
using MatchBy.Services.Conversations;
using MatchBy.Services.ImageRefresh;
using MatchBy.Services.MatchInvites;
using MatchBy.Services.Matches;
using MatchBy.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Moq;
using IEmailSender = MatchBy.Services.Email.IEmailSender;
using Match = MatchBy.Models.Match;

namespace MatchBy.UnitTests.Services.Matches;

public class MatchesServiceTests : IDisposable
{
    private readonly Mock<IFriendRepository> _friendRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IImageRefreshService> _imageRefreshServiceMock;
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly Mock<IMatchRepository> _matchesRepositoryMock;
    private readonly Mock<IConversationService> _conversationServiceMock;
    private readonly Mock<IDbContextFactory<ApplicationDbContext>> _dbContextFactoryMock;
    private readonly Mock<IValidator<CreateMatchDto>> _createMatchValidatorMock;
    private readonly Mock<IValidator<UpdateMatchDto>> _updateMatchValidatorMock;
    private readonly Mock<IMatchesInvitesService> _matchInvitesServiceMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly MatchesService _matchesService;

    public MatchesServiceTests()
    {
        _friendRepositoryMock = new Mock<IFriendRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _imageRefreshServiceMock = new Mock<IImageRefreshService>();
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        _matchesRepositoryMock = new Mock<IMatchRepository>();
        _conversationServiceMock = new Mock<IConversationService>();
        _dbContextFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        _createMatchValidatorMock = new Mock<IValidator<CreateMatchDto>>();
        _updateMatchValidatorMock = new Mock<IValidator<UpdateMatchDto>>();
        _matchInvitesServiceMock = new Mock<IMatchesInvitesService>();
        _emailSenderMock = new Mock<IEmailSender>();
        _notificationServiceMock = new Mock<INotificationService>();

        // Setup in-memory database
        string databaseName = Guid.NewGuid().ToString();
        DbContextOptions<ApplicationDbContext> dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        _dbContext = new ApplicationDbContext(dbContextOptions);

        _dbContextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationDbContext(dbContextOptions));

        // Setup validators to return valid by default
        _createMatchValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateMatchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _updateMatchValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateMatchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _matchesService = new MatchesService(
            _friendRepositoryMock.Object,
            _userRepositoryMock.Object,
            _imageRefreshServiceMock.Object,
            _conversationRepositoryMock.Object,
            _matchesRepositoryMock.Object,
            _conversationServiceMock.Object,
            _dbContextFactoryMock.Object,
            _createMatchValidatorMock.Object,
            _updateMatchValidatorMock.Object,
            _matchInvitesServiceMock.Object,
            _emailSenderMock.Object,
            _notificationServiceMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetAllMatchCountries Tests

    [Fact]
    public async Task GetAllMatchCountries_ShouldReturnCountriesList()
    {
        // Arrange
        var countries = new List<string> { "Portugal", "Spain", "France" };

        _matchesRepositoryMock
            .Setup(r => r.GetAllMatchCountries(It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(countries);

        // Act
        Result<List<string>> result = await _matchesService.GetAllMatchCountries();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(countries, result.Data);
    }

    #endregion

    #region GetAllCitiesByCountry Tests

    [Fact]
    public async Task GetAllCitiesByCountry_WithValidCountry_ShouldReturnCitiesList()
    {
        // Arrange
        string country = "Portugal";
        var cities = new List<string> { "Lisbon", "Porto", "Coimbra" };

        _matchesRepositoryMock
            .Setup(r => r.GetAllCitiesByCountry(country, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cities);

        // Act
        Result<List<string>> result = await _matchesService.GetAllCitiesByCountry(country);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(cities, result.Data);
    }

    #endregion

    #region GetMatches Tests

    [Fact]
    public async Task GetMatches_WithValidParameters_ShouldReturnMatches()
    {
        // Arrange
        var matchQueryParametersDto = new MatchQueryParametersDto
        {
            UserId = "user1",
            Page = 1,
            PageSize = 10
        };

        var matches = new List<Match>
        {
            new Match
            {
                Id = "match1",
                Location = new Location(40.7128, -74.0060, "New York", "USA"),
                Address = "123 Main St",
                MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
                Description = "Test match",
                MinimumPlayersRating = MinimumPlayersAverage.All,
                MinPlayers = 2,
                MaxPlayers = 10,
                Sport = Sports.Football,
                Status = MatchStatus.Pendent,
                Privacy = MatchPrivacy.Public,
                CreatorId = "creator1",
                Creator = new ApplicationUser { Id = "creator1", UserName = "creator" },
                Participants = new List<ApplicationUser>(),
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        var paginationResponse = new PaginationResponse<List<Match>>
        {
            Data = matches,
            Page = 1,
            PageSize = 10,
            TotalCount = 1
        };

        var receivedInvitesResult = Result<PaginationResponse<List<MatchInviteDto>>>.Ok(
            new PaginationResponse<List<MatchInviteDto>>
            {
                Data = new List<MatchInviteDto>
                {
                    new MatchInviteDto
                    {
                        Id = "invite1",
                        MatchId = "match1",
                        Status = InviteStatus.Pending,
                        Content = "Join my match!",
                        SenderId = "sender1",
                        ReceiverId = "user1",
                        ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    new MatchInviteDto
                    {
                        Id = "invite2",
                        MatchId = "match2",
                        Status = InviteStatus.Accepted,
                        Content = "Join my match!",
                        SenderId = "sender2",
                        ReceiverId = "user1",
                        ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                        CreatedAtUtc = DateTime.UtcNow
                    }
                },
                Page = 1,
                PageSize = int.MaxValue,
                TotalCount = 2
            });

        _matchInvitesServiceMock
            .Setup(s => s.GetReceivedInvites(matchQueryParametersDto.UserId!, 1, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(receivedInvitesResult);

        _matchesRepositoryMock
            .Setup(r => r.GetMatches(matchQueryParametersDto, It.IsAny<List<string>>(), It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        _imageRefreshServiceMock
            .Setup(s => s.RefreshUserProfileImageAsync(It.IsAny<ApplicationUser>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<PaginationResponse<List<MatchDto>>> result = await _matchesService.GetMatches(matchQueryParametersDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
        Assert.Equal("match1", result.Data.Data[0].Id);

        _matchInvitesServiceMock.Verify(s => s.GetReceivedInvites(matchQueryParametersDto.UserId!, 1, int.MaxValue, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMatches_WithFailedInviteRetrieval_ShouldReturnFailure()
    {
        // Arrange
        var matchQueryParametersDto = new MatchQueryParametersDto
        {
            UserId = "user1",
            Page = 1,
            PageSize = 10
        };

        var receivedInvitesResult = Result<PaginationResponse<List<MatchInviteDto>>>.Fail("Failed to get invites");

        _matchInvitesServiceMock
            .Setup(s => s.GetReceivedInvites(matchQueryParametersDto.UserId!, 1, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(receivedInvitesResult);

        // Act
        Result<PaginationResponse<List<MatchDto>>> result = await _matchesService.GetMatches(matchQueryParametersDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Failed to get invites", result.ErrorMessages[0]);
    }

    #endregion

    #region GetMatchById Tests

    [Fact]
    public async Task GetMatchById_WithValidMatch_ShouldReturnMatchDto()
    {
        // Arrange
        string matchId = "match1";
        string userId = "user1";

        var match = new Match
        {
            Id = matchId,
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "123 Main St",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test match",
            MinimumPlayersRating = MinimumPlayersAverage.All,
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator1",
            Participants = new List<ApplicationUser>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchInvitesServiceMock
            .Setup(s => s.GetMatchInvite(matchId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MatchInviteDto>.Fail("No invite found"));

        _matchesRepositoryMock
            .Setup(r => r.GetByIdAsync(matchId, userId, false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        // Act
        Result<MatchDto> result = await _matchesService.GetMatchById(matchId, userId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(matchId, result.Data.Id);
    }

    [Fact]
    public async Task GetMatchById_WithNonExistentMatch_ShouldReturnFailure()
    {
        // Arrange
        string matchId = "nonexistent";
        string userId = "user1";

        _matchInvitesServiceMock
            .Setup(s => s.GetMatchInvite(matchId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MatchInviteDto>.Fail("No invite found"));

        _matchesRepositoryMock
            .Setup(r => r.GetByIdAsync(matchId, userId, false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Match?)null);

        // Act
        Result<MatchDto> result = await _matchesService.GetMatchById(matchId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Match with id {matchId} not found", result.ErrorMessages[0]);
    }

    #endregion

    #region CreateMatch Tests

    [Fact]
    public async Task CreateMatch_WithValidDto_ShouldCreateMatchAndSendNotifications()
    {
        // Arrange
        var createMatchDto = new CreateMatchDto
        {
            CreatorId = "creator1",
            Sport = Sports.Football,
            Description = "Friendly match",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Address = "Sports Center",
            Privacy = MatchPrivacy.Public,
            MaxPlayers = 10,
            MinPlayers = 2,
            MembersIds =
            [
                "user1",
                "user2"
            ],
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            MinimumPlayersRating = MinimumPlayersAverage.All
        };

        var creator = new ApplicationUser
        {
            Id = "creator1",
            UserName = "creator",
            DisplayName = "Creator",
            Email = "creator@test.com",
            EmailConfirmed = true
        };

        var friends = new List<Friend>
        {
            new() { Id = "friend1", SenderId = "creator1", ReceiverId = "friend1", Status = FriendStatus.Accepted },
            new() { Id = "friend2", SenderId = "friend2", ReceiverId = "creator1", Status = FriendStatus.Accepted }
        };

        var paginationResponse = new PaginationResponse<List<Friend>>
        {
            Data = friends,
            TotalCount = 2,
            Page = 1,
            PageSize = 1000
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(createMatchDto.CreatorId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(creator);

        _conversationServiceMock
            .Setup(s => s.CreateConversationAsync(It.IsAny<CreateConversationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Ok(new ConversationDto
            {
                Id = "conv1",
                Type = ConversationType.Private,
                CreatorId = null,
                CreatedAtUtc = default,
                MessagesCount = 0
            }));

        _friendRepositoryMock
            .Setup(r => r.GetUserFriends(creator.Id, It.IsAny<ApplicationDbContext>(), 1, 1000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        _notificationServiceMock
            .Setup(s => s.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        _matchesRepositoryMock
            .Setup(r => r.Add(It.IsAny<Match>(), It.IsAny<ApplicationDbContext>()))
            .Callback<Match, ApplicationDbContext>((m, db) =>
            {
                m.Id = "match1";
            });

        // Act
        Result<MatchDto> result = await _matchesService.CreateMatch(createMatchDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("match1", result.Data.Id);

        _matchInvitesServiceMock.Verify(s => s.CreateInvite(It.IsAny<CreateMatchInviteDto>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _notificationServiceMock.Verify(s => s.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateMatch_WithInvalidValidation_ShouldReturnFailure()
    {
        // Arrange
        var createMatchDto = new CreateMatchDto
        {
            CreatorId = "creator1",
            Sport = Sports.Football,
            Description = "Invalid match",
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "Rua de cima",
            MatchDateTimeUtc = default,
            MinimumPlayersRating = MinimumPlayersAverage.All,
            MinPlayers = 0,
            MaxPlayers = 0,
            Privacy = MatchPrivacy.Private
        };

        var validationResult = new FluentValidation.Results.ValidationResult(new[]
        {
            new FluentValidation.Results.ValidationFailure("Sport", "Sport is required")
        });

        _createMatchValidatorMock
            .Setup(v => v.ValidateAsync(createMatchDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        Result<MatchDto> result = await _matchesService.CreateMatch(createMatchDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Sport is required", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task CreateMatch_WithNonExistentCreator_ShouldReturnFailure()
    {
        // Arrange
        var createMatchDto = new CreateMatchDto
        {
            CreatorId = "nonexistent",
            Sport = Sports.Football,
            Description = "Test match",
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "Rua de cima",
            MatchDateTimeUtc = default,
            MinimumPlayersRating = MinimumPlayersAverage.All,
            MinPlayers = 0,
            MaxPlayers = 0,
            Privacy = MatchPrivacy.Private
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        Result<MatchDto> result = await _matchesService.CreateMatch(createMatchDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Creator with id nonexistent not found", result.ErrorMessages[0]);
    }

    #endregion

    #region UpdateMatch Tests

    [Fact]
    public async Task UpdateMatch_WithValidDto_ShouldUpdateMatch()
    {
        // Arrange
        var updateMatchDto = new UpdateMatchDto
        {
            MatchId = "match1",
            UserId = "creator1",
            Description = "Updated description",
            Address = "New Address",
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            MatchDateTimeUtc = default,
            MinimumPlayersRating = MinimumPlayersAverage.All,
            MinPlayers = 0,
            MaxPlayers = 0,
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Private
        };

        var existingMatch = new Match
        {
            Id = "match1",
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "Old address",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Old description",
            MinimumPlayersRating = MinimumPlayersAverage.All,
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator1",
            Participants = new List<ApplicationUser>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Matches.AddAsync(existingMatch);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<bool> result = await _matchesService.UpdateMatch(updateMatchDto);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
    }

    [Fact]
    public async Task UpdateMatch_WithNonCreator_ShouldReturnFailure()
    {
        // Arrange
        var updateMatchDto = new UpdateMatchDto
        {
            MatchId = "match1",
            UserId = "noncreator",
            Description = "Updated description",
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "Rua de cima",
            MatchDateTimeUtc = default,
            MinimumPlayersRating = MinimumPlayersAverage.All,
            MinPlayers = 0,
            MaxPlayers = 0,
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Private
        };

        _matchesRepositoryMock
            .Setup(r => r.GetByIdAsync(updateMatchDto.MatchId, updateMatchDto.UserId, false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Match?)null);

        // Act
        Result<bool> result = await _matchesService.UpdateMatch(updateMatchDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Match with id match1 not found or user noncreator is not the creator", result.ErrorMessages[0]);
    }

    #endregion

    #region DeleteMatch Tests

    [Fact]
    public async Task DeleteMatch_WithValidMatch_ShouldDeleteMatch()
    {
        // Arrange
        string matchId = "match1";
        string userId = "creator1";

        var match = new Match
        {
            Id = matchId,
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "123 Main St",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test match",
            MinimumPlayersRating = MinimumPlayersAverage.All,
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = userId,
            ConversationId = "conv1",
            Conversation = new Conversation { Id = "conv1", Type = ConversationType.Match },
            Participants = new List<ApplicationUser>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchesRepositoryMock
            .Setup(r => r.GetByIdAsync(matchId, userId, false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        _conversationRepositoryMock
            .Setup(r => r.Remove(It.IsAny<Conversation>(), It.IsAny<ApplicationDbContext>()))
            .Verifiable();

        // Act
        Result<bool> result = await _matchesService.DeleteMatch(matchId, userId);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);

        _matchesRepositoryMock.Verify(r => r.Remove(match, It.IsAny<ApplicationDbContext>()), Times.Once);
        _conversationRepositoryMock.Verify(r => r.Remove(It.IsAny<Conversation>(), It.IsAny<ApplicationDbContext>()), Times.Once);
    }

    [Fact]
    public async Task DeleteMatch_WithNonCreator_ShouldReturnFailure()
    {
        // Arrange
        string matchId = "match1";
        string userId = "noncreator";

        _matchesRepositoryMock
            .Setup(r => r.GetByIdAsync(matchId, userId, false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Match?)null);

        // Act
        Result<bool> result = await _matchesService.DeleteMatch(matchId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Match with id match1 not found or user noncreator is not the creator", result.ErrorMessages[0]);
    }

    #endregion

    #region JoinMatch Tests

    [Fact]
    public async Task JoinMatch_WithValidInvite_ShouldAddUserToMatch()
    {
        // Arrange
        string matchId = "match1";
        string userId = "user1";

        var matchInvite = new MatchInviteDto
        {
            Id = "invite1",
            MatchId = matchId,
            ReceiverId = userId,
            Status = InviteStatus.Pending,
            Content = "Join my match!",
            SenderId = "sender1",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        var match = new Match
        {
            Id = matchId,
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "123 Main St",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test match",
            MinimumPlayersRating = MinimumPlayersAverage.All,
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator1",
            Conversation = new Conversation
            {
                Id = "conv1",
                Type = ConversationType.Match,
                CreatorId = "creator1",
                Participants = new List<ApplicationUser>(),
                CreatedAtUtc = DateTime.UtcNow
            },
            Participants = new List<ApplicationUser>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "user1",
            DisplayName = "User 1",
            Email = "user1@test.com",
            EmailConfirmed = true,
            Rating = 5
        };

        _matchInvitesServiceMock
            .Setup(s => s.GetMatchInvite(matchId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MatchInviteDto>.Ok(matchInvite));

        _matchesRepositoryMock
            .Setup(r => r.GetByIdAsync(matchId, userId, true, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _matchInvitesServiceMock
            .Setup(s => s.AcceptInvite("invite1", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MatchInviteDto>.Ok(matchInvite));

        _notificationServiceMock
            .Setup(s => s.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        // Act
        Result<MatchDto> result = await _matchesService.JoinMatch(matchId, userId);

        // Assert
        Assert.True(result.Success);
        Assert.Contains(user, match.Conversation!.Participants);

        _matchInvitesServiceMock.Verify(s => s.AcceptInvite("invite1", userId, It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(s => s.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task JoinMatch_WithCancelledMatch_ShouldReturnFailure()
    {
        // Arrange
        string matchId = "match1";
        string userId = "user1";

        var matchInvite = new MatchInviteDto
        {
            Id = "invite1",
            MatchId = matchId,
            ReceiverId = userId,
            Status = InviteStatus.Pending,
            Content = "Join my match!",
            SenderId = "sender1",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        var match = new Match
        {
            Id = matchId,
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "123 Main St",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test match",
            MinimumPlayersRating = MinimumPlayersAverage.All,
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Cancelled,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator1",
            Participants = new List<ApplicationUser>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchInvitesServiceMock
            .Setup(s => s.GetMatchInvite(matchId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MatchInviteDto>.Ok(matchInvite));

        _matchesRepositoryMock
            .Setup(r => r.GetByIdAsync(matchId, userId, true, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        // Act
        Result<MatchDto> result = await _matchesService.JoinMatch(matchId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Match with id {matchId} is not open for joining", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task JoinMatch_WithUserRatingBelowMinimum_ShouldReturnFailure()
    {
        // Arrange
        string matchId = "match1";
        string userId = "user1";

        var matchInvite = new MatchInviteDto
        {
            Id = "invite1",
            MatchId = matchId,
            ReceiverId = userId,
            Status = InviteStatus.Pending,
            Content = "Join my match!",
            SenderId = "sender1",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        var match = new Match
        {
            Id = matchId,
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "123 Main St",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test match",
            MinimumPlayersRating = MinimumPlayersAverage.FiveStars,
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator1",
            Participants = new List<ApplicationUser>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "user1",
            DisplayName = "User 1",
            Rating = 2 // Below 5-star requirement
        };

        _matchInvitesServiceMock
            .Setup(s => s.GetMatchInvite(matchId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MatchInviteDto>.Ok(matchInvite));

        _matchesRepositoryMock
            .Setup(r => r.GetByIdAsync(matchId, userId, true, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _matchInvitesServiceMock
            .Setup(s => s.AcceptInvite("invite1", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MatchInviteDto>.Fail("User rating below minimum"));

        // Act
        Result<MatchDto> result = await _matchesService.JoinMatch(matchId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("User with id user1 does not meet the minimum player rating requirement", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task JoinMatch_WithPublicMatchAndNoInvite_ShouldAddUserDirectly()
    {
        // Arrange
        string matchId = "match1";
        string userId = "user1";

        var match = new Match
        {
            Id = matchId,
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "123 Main St",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test match",
            MinimumPlayersRating = MinimumPlayersAverage.All,
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator1",
            Creator = new ApplicationUser { Id = "creator1", UserName = "creator" },
            Conversation = new Conversation
            {
                Id = "conv1",
                Type = ConversationType.Match,
                CreatorId = "creator1",
                Participants = new List<ApplicationUser>(),
                CreatedAtUtc = DateTime.UtcNow
            },
            Participants = new List<ApplicationUser>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "user1",
            DisplayName = "User 1",
            Rating = 4
        };

        _matchInvitesServiceMock
            .Setup(s => s.GetMatchInvite(matchId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MatchInviteDto>.Fail("No invite found"));

        _matchesRepositoryMock
            .Setup(r => r.GetByIdAsync(matchId, userId, false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _matchInvitesServiceMock
            .Setup(s => s.GetMatchInvite(matchId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MatchInviteDto>.Fail("No invite found"));

        _imageRefreshServiceMock
            .Setup(s => s.RefreshUserProfileImageAsync(It.IsAny<ApplicationUser>()))
            .Returns(Task.CompletedTask);

        _notificationServiceMock
            .Setup(s => s.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        // Act
        Result<MatchDto> result = await _matchesService.JoinMatch(matchId, userId);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task JoinMatch_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        string matchId = "match1";
        string userId = "nonexistent";

        var matchInvite = new MatchInviteDto
        {
            Id = "invite1",
            MatchId = matchId,
            ReceiverId = userId,
            Status = InviteStatus.Pending,
            Content = "Join my match!",
            SenderId = "sender1",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        var match = new Match
        {
            Id = matchId,
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "123 Main St",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test match",
            MinimumPlayersRating = MinimumPlayersAverage.All,
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator1",
            Participants = new List<ApplicationUser>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchInvitesServiceMock
            .Setup(s => s.GetMatchInvite(matchId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MatchInviteDto>.Ok(matchInvite));

        _matchesRepositoryMock
            .Setup(r => r.GetByIdAsync(matchId, userId, true, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        Result<MatchDto> result = await _matchesService.JoinMatch(matchId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"User with id {userId} not found", result.ErrorMessages[0]);
    }

    #endregion

    #region ConfirmMatch Tests

    [Fact]
    public async Task ConfirmMatch_WithValidMatch_ShouldConfirmMatch()
    {
        // Arrange
        string matchId = "match1";
        string userId = "creator1";

        var match = new Match
        {
            Id = matchId,
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "123 Main St",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test match",
            MinimumPlayersRating = MinimumPlayersAverage.All,
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = userId,
            Creator = new ApplicationUser { Id = userId, UserName = "creator" },
            Participants = new List<ApplicationUser>
            {
                new() { Id = "user1", UserName = "user1" },
                new() { Id = "user2", UserName = "user2" }
            },
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchesRepositoryMock
            .Setup(r => r.GetByIdAsync(matchId, userId, false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        _matchInvitesServiceMock
            .Setup(s => s.GetMatchInvite(matchId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MatchInviteDto>.Fail("No invite found"));

        _notificationServiceMock
            .Setup(s => s.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        // Act
        Result<MatchDto> result = await _matchesService.ConfirmMatch(matchId, userId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(MatchStatus.Confirmed, match.Status);

        _notificationServiceMock.Verify(s => s.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ConfirmMatch_WithInsufficientPlayers_ShouldReturnFailure()
    {
        // Arrange
        string matchId = "match1";
        string userId = "creator1";

        var match = new Match
        {
            Id = matchId,
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "123 Main St",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test match",
            MinimumPlayersRating = MinimumPlayersAverage.All,
            MinPlayers = 4,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = userId,
            Participants = new List<ApplicationUser>
            {
                new() { Id = "user1", UserName = "user1" },
                new() { Id = "user2", UserName = "user2" }
            },
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchesRepositoryMock
            .Setup(r => r.GetByIdAsync(matchId, userId, false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        // Act
        Result<MatchDto> result = await _matchesService.ConfirmMatch(matchId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Cant confirm match with id {matchId} because it doesnt have the required number of players", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task ConfirmMatch_WithNonCreator_ShouldReturnFailure()
    {
        // Arrange
        string matchId = "match1";
        string userId = "noncreator";

        _matchesRepositoryMock
            .Setup(r => r.GetByIdAsync(matchId, userId, false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Match?)null);

        // Act
        Result<MatchDto> result = await _matchesService.ConfirmMatch(matchId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Match with id {matchId} not found or user {userId} is not the creator", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task ConfirmMatch_WithAlreadyConfirmedMatch_ShouldReturnFailure()
    {
        // Arrange
        string matchId = "match1";
        string userId = "creator1";

        var match = new Match
        {
            Id = matchId,
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "123 Main St",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test match",
            MinimumPlayersRating = MinimumPlayersAverage.All,
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Confirmed,
            Privacy = MatchPrivacy.Public,
            CreatorId = userId,
            Participants = new List<ApplicationUser>
            {
                new() { Id = "user1", UserName = "user1" },
                new() { Id = "user2", UserName = "user2" }
            },
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchesRepositoryMock
            .Setup(r => r.GetByIdAsync(matchId, userId, false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        // Act
        Result<MatchDto> result = await _matchesService.ConfirmMatch(matchId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Cant confirm match with id {matchId} because its not in pendent status", result.ErrorMessages[0]);
    }

    #endregion

    #region LeaveMatch Tests

    [Fact]
    public async Task LeaveMatch_WithCreator_ShouldCancelMatchAndNotifyParticipants()
    {
        // Arrange
        string matchId = "match1";
        string userId = "creator1";

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "creator",
            DisplayName = "Creator",
            Email = "creator@test.com",
            EmailConfirmed = true
        };

        var participants = new List<ApplicationUser>
        {
            new() { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true },
            new() { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true }
        };

        var match = new Match
        {
            Id = matchId,
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "123 Main St",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test match",
            MinimumPlayersRating = MinimumPlayersAverage.All,
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = userId,
            Participants = new List<ApplicationUser>(participants),
            ConversationId = "conv1",
            CreatedAtUtc = DateTime.UtcNow
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _matchesRepositoryMock
            .Setup(r => r.GetByIdAsync(matchId, userId, false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        _conversationServiceMock
            .Setup(s => s.DeleteConversationAsync("conv1", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        _notificationServiceMock
            .Setup(s => s.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        // Act
        Result<bool> result = await _matchesService.LeaveMatch(matchId, userId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(MatchStatus.Cancelled, match.Status);

        _conversationServiceMock.Verify(s => s.DeleteConversationAsync("conv1", userId, It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(s => s.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task LeaveMatch_WithParticipant_ShouldRemoveUserFromMatch()
    {
        // Arrange
        string matchId = "match1";
        string userId = "user1";

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "user1",
            DisplayName = "User 1",
            Email = "user1@test.com",
            EmailConfirmed = true
        };

        var participants = new List<ApplicationUser>
        {
            user,
            new() { Id = "user2", UserName = "user2", DisplayName = "User 2" }
        };

        var match = new Match
        {
            Id = matchId,
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "123 Main St",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test match",
            MinimumPlayersRating = MinimumPlayersAverage.All,
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator1",
            Participants = new List<ApplicationUser>(participants),
            ConversationId = "conv1",
            CreatedAtUtc = DateTime.UtcNow
        };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Match,
            Participants = new List<ApplicationUser>(participants)
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _matchesRepositoryMock
            .Setup(r => r.GetByIdAsync(matchId, userId, false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        // Act
        Result<bool> result = await _matchesService.LeaveMatch(matchId, userId);

        // Assert
        Assert.True(result.Success);
        Assert.DoesNotContain(user, match.Participants);
    }

    #endregion

    #region GetMatchesForUser Tests

    [Fact]
    public async Task GetMatchesForUser_WithValidUserId_ShouldReturnUserMatches()
    {
        // Arrange
        string userId = "user1";
        string q = "football";
        int page = 1;
        int pageSize = 5;

        var matches = new List<Match>
        {
            new() { Id = "match1", Sport = Sports.Football, CreatorId = userId, CreatedAtUtc = DateTime.UtcNow }
        };

        var paginationResponse = new PaginationResponse<List<Match>>
        {
            Data = matches,
            Page = 1,
            PageSize = 5,
            TotalCount = 1
        };

        _matchesRepositoryMock
            .Setup(r => r.GetMatchesForUser(userId, It.IsAny<ApplicationDbContext>(), q, page, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        _imageRefreshServiceMock
            .Setup(s => s.RefreshUserProfileImageAsync(It.IsAny<ApplicationUser>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<PaginationResponse<List<MatchDto>>> result = await _matchesService.GetMatchesForUser(userId, q, page, pageSize);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data.Data);
        Assert.Equal("match1", result.Data.Data[0].Id);
    }

    #region GetMatchesExceptUser Tests

    [Fact]
    public async Task GetMatchesExceptUser_WithValidParameters_ShouldReturnMatchesExceptUser()
    {
        // Arrange
        string userId = "user1";
        string q = null;
        int page = 1;
        int pageSize = 5;

        var matches = new List<Match>
        {
            new Match { Id = "match1", CreatorId = "user2", Sport = Sports.Football, MatchDateTimeUtc = DateTime.UtcNow.AddDays(1) },
            new Match { Id = "match2", CreatorId = "user3", Sport = Sports.Basketball, MatchDateTimeUtc = DateTime.UtcNow.AddDays(2) }
        };

        var paginationResponse = new PaginationResponse<List<Match>>
        {
            Data = matches,
            Page = 1,
            PageSize = 5,
            TotalCount = 2
        };

        _matchesRepositoryMock.Setup(x => x.GetMatchesExceptUser(userId, It.IsAny<ApplicationDbContext>(), q, page, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<MatchDto>>> result = await _matchesService.GetMatchesExceptUser(userId, q, page, pageSize);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Data.Count);
        Assert.Equal("match1", result.Data.Data[0].Id);
        Assert.Equal("match2", result.Data.Data[1].Id);
        Assert.Equal(1, result.Data.Page);
        Assert.Equal(5, result.Data.PageSize);
        Assert.Equal(2, result.Data.TotalCount);

        _matchesRepositoryMock.Verify(x => x.GetMatchesExceptUser(userId, It.IsAny<ApplicationDbContext>(), q, page, pageSize, It.IsAny<CancellationToken>()), Times.Once);
        _imageRefreshServiceMock.Verify(x => x.RefreshUserProfileImageAsync(It.IsAny<ApplicationUser>()), Times.Exactly(2)); // 2 creators + 0 participants each
    }

    [Fact]
    public async Task GetMatchesExceptUser_WithQuery_ShouldPassQueryToRepository()
    {
        // Arrange
        string userId = "user1";
        string q = "football";
        int page = 1;
        int pageSize = 5;

        var matches = new List<Match>();
        var paginationResponse = new PaginationResponse<List<Match>>
        {
            Data = matches,
            Page = 1,
            PageSize = 5,
            TotalCount = 0
        };

        _matchesRepositoryMock.Setup(x => x.GetMatchesExceptUser(userId, It.IsAny<ApplicationDbContext>(), q, page, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<MatchDto>>> result = await _matchesService.GetMatchesExceptUser(userId, q, page, pageSize);

        // Assert
        Assert.True(result.Success);
        _matchesRepositoryMock.Verify(x => x.GetMatchesExceptUser(userId, It.IsAny<ApplicationDbContext>(), q, page, pageSize, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetMatchesUserAttending Tests

    [Fact]
    public async Task GetMatchesUserAttending_WithValidParameters_ShouldReturnMatchesUserAttending()
    {
        // Arrange
        string userId = "user1";
        string q = null;
        int page = 1;
        int pageSize = 5;

        var matches = new List<Match>
        {
            new Match { Id = "match1", CreatorId = "user1", Sport = Sports.Football, MatchDateTimeUtc = DateTime.UtcNow.AddDays(1) },
            new Match { Id = "match2", CreatorId = "user1", Sport = Sports.Basketball, MatchDateTimeUtc = DateTime.UtcNow.AddDays(2) }
        };

        var paginationResponse = new PaginationResponse<List<Match>>
        {
            Data = matches,
            Page = 1,
            PageSize = 5,
            TotalCount = 2
        };

        _matchesRepositoryMock.Setup(x => x.GetMatchesUserAttending(userId, It.IsAny<ApplicationDbContext>(), q, page, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<MatchDto>>> result = await _matchesService.GetMatchesUserAttending(userId, q, page, pageSize);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Data.Count);
        Assert.Equal("match1", result.Data.Data[0].Id);
        Assert.Equal("match2", result.Data.Data[1].Id);
        Assert.Equal(1, result.Data.Page);
        Assert.Equal(5, result.Data.PageSize);
        Assert.Equal(2, result.Data.TotalCount);

        _matchesRepositoryMock.Verify(x => x.GetMatchesUserAttending(userId, It.IsAny<ApplicationDbContext>(), q, page, pageSize, It.IsAny<CancellationToken>()), Times.Once);
        _imageRefreshServiceMock.Verify(x => x.RefreshUserProfileImageAsync(It.IsAny<ApplicationUser>()), Times.Exactly(2)); // 2 creators + 0 participants each
    }

    [Fact]
    public async Task GetMatchesUserAttending_WithParticipants_ShouldRefreshAllUserImages()
    {
        // Arrange
        string userId = "user1";
        string q = null;
        int page = 1;
        int pageSize = 5;

        var participant1 = new ApplicationUser { Id = "user2", UserName = "user2" };
        var participant2 = new ApplicationUser { Id = "user3", UserName = "user3" };

        var matches = new List<Match>
        {
            new Match
            {
                Id = "match1",
                CreatorId = "user1",
                Sport = Sports.Football,
                MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
                Participants = [participant1, participant2]
            }
        };

        var paginationResponse = new PaginationResponse<List<Match>>
        {
            Data = matches,
            Page = 1,
            PageSize = 5,
            TotalCount = 1
        };

        _matchesRepositoryMock.Setup(x => x.GetMatchesUserAttending(userId, It.IsAny<ApplicationDbContext>(), q, page, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<MatchDto>>> result = await _matchesService.GetMatchesUserAttending(userId, q, page, pageSize);

        // Assert
        Assert.True(result.Success);
        _imageRefreshServiceMock.Verify(x => x.RefreshUserProfileImageAsync(It.IsAny<ApplicationUser>()), Times.Exactly(3)); // 1 creator + 2 participants
    }

    #endregion

    #endregion
}