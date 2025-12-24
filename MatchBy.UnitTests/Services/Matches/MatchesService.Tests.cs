using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.DTOs.Match;
using MatchBy.DTOs.MatchInvite;
using MatchBy.DTOs.Notification;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Repositories.ChatConversation;
using MatchBy.Repositories.Match;
using MatchBy.Repositories.User;
using MatchBy.Services.Conversations;
using MatchBy.Services.ImageRefresh;
using MatchBy.Services.MatchInvites;
using MatchBy.Services.Matches;
using MatchBy.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Moq;
using Match = MatchBy.Models.Match;
using IEmailSender = MatchBy.Services.Email.IEmailSender;

namespace MatchBy.UnitTests.Services.Matches;

public class MatchesServiceTests : IDisposable
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IMatchRepository> _matchRepositoryMock;
    private readonly Mock<IImageRefreshService> _imageRefreshServiceMock;
    private readonly Mock<IConversationService> _conversationServiceMock;
    private readonly Mock<IMatchesInvitesService> _matchInvitesServiceMock;
    private readonly Mock<IValidator<CreateMatchDto>> _createMatchValidatorMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly MatchesService _matchesService;

    public MatchesServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _matchRepositoryMock = new Mock<IMatchRepository>();
        var conversationRepositoryMock = new Mock<IConversationRepository>();
        _imageRefreshServiceMock = new Mock<IImageRefreshService>();
        _conversationServiceMock = new Mock<IConversationService>();
        _matchInvitesServiceMock = new Mock<IMatchesInvitesService>();
        _createMatchValidatorMock = new Mock<IValidator<CreateMatchDto>>();
        var updateMatchValidatorMock = new Mock<IValidator<UpdateMatchDto>>();
        var emailSenderMock = new Mock<IEmailSender>();
        var notificationServiceMock = new Mock<INotificationService>();
        var dbContextFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();

        // Setup in-memory database
        string databaseName = Guid.NewGuid().ToString();
        DbContextOptions<ApplicationDbContext> dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        _dbContext = new ApplicationDbContext(dbContextOptions);

        dbContextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationDbContext(dbContextOptions));

        // Setup image refresh service
        _imageRefreshServiceMock
            .Setup(s => s.RefreshUserProfileImageAsync(It.IsAny<ApplicationUser>()))
            .Returns(Task.CompletedTask);

        // Setup validators to return valid by default
        _createMatchValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateMatchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        updateMatchValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateMatchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        

        // Setup notification service
        notificationServiceMock
            .Setup(s => s.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        _matchesService = new MatchesService(
            _userRepositoryMock.Object,
            _imageRefreshServiceMock.Object,
            conversationRepositoryMock.Object,
            _matchRepositoryMock.Object,
            _conversationServiceMock.Object,
            dbContextFactoryMock.Object,
            _createMatchValidatorMock.Object,
            updateMatchValidatorMock.Object,
            _matchInvitesServiceMock.Object,
            emailSenderMock.Object,
            notificationServiceMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetAllMatchCountries Tests

    [Fact]
    public async Task GetAllMatchCountries_ShouldReturnCountries()
    {
        // Arrange
        var countries = new List<string> { "Country1", "Country2" };

        _matchRepositoryMock
            .Setup(r => r.GetAllMatchCountries(It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(countries);

        // Act
        Result<List<string>> result = await _matchesService.GetAllMatchCountries();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Count);
        Assert.Contains("Country1", result.Data);
        Assert.Contains("Country2", result.Data);
    }

    #endregion

    #region GetAllCitiesByCountry Tests

    [Fact]
    public async Task GetAllCitiesByCountry_WithValidCountry_ShouldReturnCities()
    {
        // Arrange
        var cities = new List<string> { "City1", "City2" };

        _matchRepositoryMock
            .Setup(r => r.GetAllCitiesByCountry("Country1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cities);

        // Act
        Result<List<string>> result = await _matchesService.GetAllCitiesByCountry("Country1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Count);
    }

    #endregion

    #region GetMatches Tests

    [Fact]
    public async Task GetMatches_WithValidQuery_ShouldReturnMatches()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };

        var matches = new List<Match>
        {
            new()
            {
                Id = "match1",
                CreatorId = "creator1",
                Description = "Match 1",
                Address = "Address",
                Location = new Location(0, 0, "City1", "Country1"),
                MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
                MinPlayers = 2,
                MaxPlayers = 10,
                Sport = Sports.Football,
                Status = MatchStatus.Pendent,
                Privacy = MatchPrivacy.Public,
                CreatedAtUtc = DateTime.UtcNow,
                Creator = creator
            }
        };

        var paginationResponse = new PaginationResponse<List<Match>>
        {
            Data = matches,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        var queryParams = new MatchQueryParametersDto
        {
            Page = 1,
            PageSize = 10
        };

        _matchInvitesServiceMock
            .Setup(s => s.GetReceivedInvites(It.IsAny<string>(), 1, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginationResponse<List<MatchInviteDto>>>.Ok(new PaginationResponse<List<MatchInviteDto>>
            {
                Data = new List<MatchInviteDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = int.MaxValue
            }));

        _matchRepositoryMock
            .Setup(r => r.GetMatches(queryParams, It.IsAny<List<string>>(), It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<MatchDto>>> result = await _matchesService.GetMatches(queryParams);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
        _imageRefreshServiceMock.Verify(s => s.RefreshUserProfileImageAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    #endregion

    #region GetMatchById Tests

    [Fact]
    public async Task GetMatchById_WithValidId_ShouldReturnMatch()
    {
        // Arrange
        var match = new Match
        {
            Id = "match1",
            CreatorId = "creator1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchInvitesServiceMock
            .Setup(s => s.GetMatchInvite("match1", "user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MatchInviteDto>.Fail("Not found"));

        _matchRepositoryMock
            .Setup(r => r.GetByIdAsync("match1", "user1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        // Act
        Result<MatchDto> result = await _matchesService.GetMatchById("match1", "user1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("match1", result.Data.Id);
    }

    [Fact]
    public async Task GetMatchById_WithNonExistentId_ShouldReturnFailure()
    {
        // Arrange
        _matchInvitesServiceMock
            .Setup(s => s.GetMatchInvite("nonexistent", "user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MatchInviteDto>.Fail("Not found"));

        _matchRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", "user1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Match?)null);

        // Act
        Result<MatchDto> result = await _matchesService.GetMatchById("nonexistent", "user1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessages[0]);
    }

    #endregion

    #region CreateMatch Tests

    [Fact]
    public async Task CreateMatch_WithValidDto_ShouldCreateMatch()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };

        await _dbContext.Users.AddAsync(creator);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreateMatchDto
        {
            CreatorId = "creator1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0,
                0,
                "City",
                "Country"),
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Public,
            MembersIds = [],
            MinimumPlayersRating = MinimumPlayersAverage.All
        };

        _conversationServiceMock
            .Setup(s => s.CreateConversationAsync(It.IsAny<CreateConversationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Ok(new ConversationDto
            {
                Id = "conv1",
                Type = ConversationType.Match,
                CreatorId = "creator1",
                CreatedAtUtc = DateTime.UtcNow,
                MessagesCount = 0
            }));

        // Act
        Result<MatchDto> result = await _matchesService.CreateMatch(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        _conversationServiceMock.Verify(s => s.CreateConversationAsync(It.IsAny<CreateConversationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateMatch_WithInvalidValidation_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateMatchDto
        {
            CreatorId = "creator1",
            Description = "",
            Address = "Test Address",
            Location = new Location(0,
                0,
                "City",
                "Country"),
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Public,
            MembersIds = [], 
            MinimumPlayersRating = MinimumPlayersAverage.All
        };

        var validationResult = new FluentValidation.Results.ValidationResult([
            new FluentValidation.Results.ValidationFailure("Description", "Description is required")
        ]);

        _createMatchValidatorMock
            .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        Result<MatchDto> result = await _matchesService.CreateMatch(createDto);

        // Assert
        Assert.False(result.Success);
    }

    #endregion

    #region UpdateMatch Tests

    [Fact]
    public async Task UpdateMatch_WithValidDto_ShouldUpdateMatch()
    {
        // Arrange
        var match = new Match
        {
            Id = "match1",
            CreatorId = "creator1",
            Description = "Old Description",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow
        };

        var updateDto = new UpdateMatchDto
        {
            Description = "New Description",
            Address = "New Address",
            Location = new Location(0,
                0,
                "NewCity",
                "NewCountry"),
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(2),
            MinPlayers = 3,
            MaxPlayers = 12,
            Sport = Sports.Basketball,
            UserId = "creator1",
            MatchId = "match1",
            MinimumPlayersRating = MinimumPlayersAverage.All,
            Privacy = MatchPrivacy.Private
        };

        _matchRepositoryMock
            .Setup(r => r.GetByIdAsync("match1", "creator1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        // Act
        Result<bool> result = await _matchesService.UpdateMatch(updateDto);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
    }

    #endregion

    #region DeleteMatch Tests

    [Fact]
    public async Task DeleteMatch_WithValidMatchAndCreator_ShouldDeleteMatch()
    {
        // Arrange
        var match = new Match
        {
            Id = "match1",
            CreatorId = "creator1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchRepositoryMock
            .Setup(r => r.GetByIdAsync("match1", "creator1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        // Act
        Result<bool> result = await _matchesService.DeleteMatch("match1", "creator1");

        // Assert
        Assert.True(result.Success);
        _matchRepositoryMock.Verify(r => r.Remove(match, It.IsAny<ApplicationDbContext>()), Times.Once);
    }

    [Fact]
    public async Task DeleteMatch_WithNonCreator_ShouldReturnFailure()
    {
        // Arrange
        _matchRepositoryMock
            .Setup(r => r.GetByIdAsync("match1", "user2", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Match?)null);

        // Act
        Result<bool> result = await _matchesService.DeleteMatch("match1", "user2");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessages[0]);
    }

    #endregion

    #region JoinMatch Tests

    [Fact]
    public async Task JoinMatch_WithValidMatch_ShouldAddParticipant()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        var user = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User", Email = "user@test.com", EmailConfirmed = true };

        var match = new Match
        {
            Id = "match1",
            CreatorId = "creator1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = [creator]
        };

        _matchRepositoryMock
            .Setup(r => r.GetByIdAsync("match1", "user1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Result<MatchDto> result = await _matchesService.JoinMatch("match1", "user1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    #endregion

    #region LeaveMatch Tests

    [Fact]
    public async Task LeaveMatch_WithValidMatchAndParticipant_ShouldRemoveParticipant()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        var user = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User", Email = "user@test.com", EmailConfirmed = true };

        var match = new Match
        {
            Id = "match1",
            CreatorId = "creator1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = [creator, user]
        };

        _matchRepositoryMock
            .Setup(r => r.GetByIdAsync("match1", "user1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        // Act
        Result<bool> result = await _matchesService.LeaveMatch("match1", "user1");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
    }

    #endregion
}
