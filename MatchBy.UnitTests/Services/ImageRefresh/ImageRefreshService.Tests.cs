using Amazon.S3;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Services.ImageRefresh;
using MatchBy.Services.S3;
using MatchBy.Settings;
using Microsoft.Extensions.Options;
using Moq;

namespace MatchBy.UnitTests.Services.ImageRefresh;

public class ImageRefreshServiceTests
{
    private readonly Mock<IS3Service> _s3ServiceMock;
    private readonly ImageRefreshService _imageRefreshService;

    public ImageRefreshServiceTests()
    {
        _s3ServiceMock = new Mock<IS3Service>();
        var s3SettingsMock = new Mock<IOptions<S3Settings>>();
        
        var s3Settings = new S3Settings
        {
            DefaultUrlExpiry = 30,
            BucketName = "test-bucket",
            Region = "us-east-1",
            AccessKey = "test-key",
            SecretKey = "test-secret"
        };
        
        s3SettingsMock.Setup(x => x.Value).Returns(s3Settings);
        
        _imageRefreshService = new ImageRefreshService(_s3ServiceMock.Object, s3SettingsMock.Object);
    }

    #region RefreshUserProfileImageAsync Tests

    [Fact]
    public async Task RefreshUserProfileImageAsync_WithNullProfileImage_ShouldNotCallS3Service()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "testuser",
            ProfileImage = null
        };

        // Act
        await _imageRefreshService.RefreshUserProfileImageAsync(user);

        // Assert
        _s3ServiceMock.Verify(
            x => x.GetPresignedUrlAsync(It.IsAny<string>(), It.IsAny<HttpVerb>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshUserProfileImageAsync_WithNullImageKey_ShouldNotCallS3Service()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "testuser",
            ProfileImage = new FileStore
            (
                "https://example.com/image.jpg",
                DateTime.UtcNow.AddDays(5), 
                null!,
                FileCategory.ProfileImage,
                FileType.Image,
                DateTime.UtcNow
            )
        };

        // Act
        await _imageRefreshService.RefreshUserProfileImageAsync(user);

        // Assert
        _s3ServiceMock.Verify(
            x => x.GetPresignedUrlAsync(It.IsAny<string>(), It.IsAny<HttpVerb>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshUserProfileImageAsync_WithValidNonExpiredUrl_ShouldNotRefresh()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "testuser",
            ProfileImage = new FileStore(
                "https://example.com/image.jpg",
                DateTime.UtcNow.AddHours(1),
                "profile-key.jpg",
                FileCategory.ProfileImage,
                FileType.Image,
                DateTime.UtcNow
            )
        };

        // Act
        await _imageRefreshService.RefreshUserProfileImageAsync(user);

        // Assert
        _s3ServiceMock.Verify(
            x => x.GetPresignedUrlAsync(It.IsAny<string>(), It.IsAny<HttpVerb>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshUserProfileImageAsync_WithExpiredUrl_ShouldRefreshUrl()
    {
        // Arrange
        string userId = "user1";
        string imageKey = "profile-key.jpg";
        string newUrl = "https://s3.amazonaws.com/new-presigned-url";
        
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testuser",
            ProfileImage = new FileStore(
                "https://example.com/old-image.jpg",
                DateTime.UtcNow.AddHours(-1),
                imageKey,
                FileCategory.ProfileImage,
                FileType.Image,
                DateTime.UtcNow
            )
        };

        _s3ServiceMock
            .Setup(x => x.GetPresignedUrlAsync($"users/{userId}/profile-pictures/{imageKey}", HttpVerb.GET))
            .ReturnsAsync(Result<string>.Ok(newUrl));

        // Act
        await _imageRefreshService.RefreshUserProfileImageAsync(user);

        // Assert
        Assert.NotNull(user.ProfileImage);
        Assert.Equal(newUrl, user.ProfileImage.Url);
        Assert.True(user.ProfileImage.ExpireDateTimeUtc > DateTime.UtcNow);
        
        _s3ServiceMock.Verify(
            x => x.GetPresignedUrlAsync($"users/{userId}/profile-pictures/{imageKey}", HttpVerb.GET),
            Times.Once);
    }

    [Fact]
    public async Task RefreshUserProfileImageAsync_WithEmptyUrl_ShouldRefreshUrl()
    {
        // Arrange
        string userId = "user1";
        string imageKey = "profile-key.jpg";
        string newUrl = "https://s3.amazonaws.com/new-presigned-url";
        
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testuser",
            ProfileImage = new FileStore(
                string.Empty,
                DateTime.UtcNow.AddHours(1),
                imageKey,
                FileCategory.ProfileImage,
                FileType.Image,
                DateTime.UtcNow
            )
        };

        _s3ServiceMock
            .Setup(x => x.GetPresignedUrlAsync($"users/{userId}/profile-pictures/{imageKey}", HttpVerb.GET))
            .ReturnsAsync(Result<string>.Ok(newUrl));

        // Act
        await _imageRefreshService.RefreshUserProfileImageAsync(user);

        // Assert
        Assert.NotNull(user.ProfileImage);
        Assert.Equal(newUrl, user.ProfileImage.Url);
        
        _s3ServiceMock.Verify(
            x => x.GetPresignedUrlAsync($"users/{userId}/profile-pictures/{imageKey}", HttpVerb.GET),
            Times.Once);
    }

    [Fact]
    public async Task RefreshUserProfileImageAsync_WhenS3ServiceFails_ShouldNotUpdateUrl()
    {
        // Arrange
        string userId = "user1";
        string imageKey = "profile-key.jpg";
        string originalUrl = "https://example.com/old-image.jpg";
        
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testuser",
            ProfileImage = new FileStore(
                originalUrl,
                DateTime.UtcNow.AddHours(-1),
                imageKey,
                FileCategory.ProfileImage,
                FileType.Image,
                DateTime.UtcNow
            )
        };

        _s3ServiceMock
            .Setup(x => x.GetPresignedUrlAsync(It.IsAny<string>(), It.IsAny<HttpVerb>()))
            .ReturnsAsync(Result<string>.Fail("S3 service error"));

        // Act
        await _imageRefreshService.RefreshUserProfileImageAsync(user);

        // Assert
        Assert.NotNull(user.ProfileImage);
        Assert.Equal(originalUrl, user.ProfileImage.Url); // URL should not change
    }

    #endregion

    #region RefreshTeamImageAsync Tests

    [Fact]
    public async Task RefreshTeamImageAsync_WithNullImage_ShouldNotCallS3Service()
    {
        // Arrange
        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            Image = null
        };

        // Act
        await _imageRefreshService.RefreshTeamImageAsync(team);

        // Assert
        _s3ServiceMock.Verify(
            x => x.GetPresignedUrlAsync(It.IsAny<string>(), It.IsAny<HttpVerb>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshTeamImageAsync_WithNullImageKey_ShouldNotCallS3Service()
    {
        // Arrange
        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            Image = new FileStore(
                "https://example.com/team.jpg",
                DateTime.UtcNow.AddHours(1),
                null!,
                FileCategory.TeamImage,
                FileType.Image,
                DateTime.UtcNow
            )
        };

        // Act
        await _imageRefreshService.RefreshTeamImageAsync(team);

        // Assert
        _s3ServiceMock.Verify(
            x => x.GetPresignedUrlAsync(It.IsAny<string>(), It.IsAny<HttpVerb>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshTeamImageAsync_WithExpiredUrl_ShouldRefreshUrl()
    {
        // Arrange
        string teamId = "team1";
        string imageKey = "team-image.jpg";
        string newUrl = "https://s3.amazonaws.com/new-team-url";
        
        var team = new Team
        {
            Id = teamId,
            Name = "Test Team",
            Image = new FileStore(
                "https://example.com/old-team.jpg",
                DateTime.UtcNow.AddHours(-1),
                imageKey,
                FileCategory.TeamImage,
                FileType.Image,
                DateTime.UtcNow
            )
        };

        _s3ServiceMock
            .Setup(x => x.GetPresignedUrlAsync($"teams/{teamId}/image/{imageKey}", HttpVerb.GET))
            .ReturnsAsync(Result<string>.Ok(newUrl));

        // Act
        await _imageRefreshService.RefreshTeamImageAsync(team);

        // Assert
        Assert.NotNull(team.Image);
        Assert.Equal(newUrl, team.Image.Url);
        Assert.True(team.Image.ExpireDateTimeUtc > DateTime.UtcNow);
        
        _s3ServiceMock.Verify(
            x => x.GetPresignedUrlAsync($"teams/{teamId}/image/{imageKey}", HttpVerb.GET),
            Times.Once);
    }

    [Fact]
    public async Task RefreshTeamImageAsync_WhenS3ServiceFails_ShouldNotUpdateUrl()
    {
        // Arrange
        string teamId = "team1";
        string imageKey = "team-image.jpg";
        string originalUrl = "https://example.com/old-team.jpg";
        
        var team = new Team
        {
            Id = teamId,
            Name = "Test Team",
            Image = new FileStore(
                originalUrl,
                DateTime.UtcNow.AddHours(-1),
                imageKey,
                FileCategory.TeamImage,
                FileType.Image,
                DateTime.UtcNow
            )
        };

        _s3ServiceMock
            .Setup(x => x.GetPresignedUrlAsync(It.IsAny<string>(), It.IsAny<HttpVerb>()))
            .ReturnsAsync(Result<string>.Fail("S3 service error"));

        // Act
        await _imageRefreshService.RefreshTeamImageAsync(team);

        // Assert
        Assert.NotNull(team.Image);
        Assert.Equal(originalUrl, team.Image.Url);
    }

    #endregion

    #region RefreshConversationImageAsync Tests

    [Fact]
    public async Task RefreshConversationImageAsync_WithNullImage_ShouldNotCallS3Service()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            Image = null
        };

        // Act
        await _imageRefreshService.RefreshConversationImageAsync(conversation);

        // Assert
        _s3ServiceMock.Verify(
            x => x.GetPresignedUrlAsync(It.IsAny<string>(), It.IsAny<HttpVerb>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshConversationImageAsync_WithExpiredUrl_ShouldRefreshUrl()
    {
        // Arrange
        string conversationId = "conv1";
        string imageKey = "conversation-image.jpg";
        string newUrl = "https://s3.amazonaws.com/new-conversation-url";
        
        var conversation = new Conversation
        {
            Id = conversationId,
            Type = ConversationType.Team,
            Image = new FileStore(
                "https://example.com/old-conversation.jpg",
                DateTime.UtcNow.AddHours(-1),
                imageKey,
                FileCategory.ConversationImage,
                FileType.Image,
                DateTime.UtcNow
            )
        };

        _s3ServiceMock
            .Setup(x => x.GetPresignedUrlAsync($"conversations/{conversationId}/image/{imageKey}", HttpVerb.GET))
            .ReturnsAsync(Result<string>.Ok(newUrl));

        // Act
        await _imageRefreshService.RefreshConversationImageAsync(conversation);

        // Assert
        Assert.NotNull(conversation.Image);
        Assert.Equal(newUrl, conversation.Image.Url);
        Assert.True(conversation.Image.ExpireDateTimeUtc > DateTime.UtcNow);
    }

    #endregion

    #region RefreshConversationImagesAsync Tests

    [Fact]
    public async Task RefreshConversationImagesAsync_ShouldRefreshConversationImage()
    {
        // Arrange
        string conversationId = "conv1";
        string imageKey = "conversation-image.jpg";
        string newUrl = "https://s3.amazonaws.com/new-url";
        
        var conversation = new Conversation
        {
            Id = conversationId,
            Type = ConversationType.Private,
            Image = new FileStore(
                "old-url",
                DateTime.UtcNow.AddHours(-1),
                imageKey,
                FileCategory.ConversationImage,
                FileType.Image,
                DateTime.UtcNow
            ),
            Participants = new List<ApplicationUser>(),
            Creator = null
        };

        _s3ServiceMock
            .Setup(x => x.GetPresignedUrlAsync($"conversations/{conversationId}/image/{imageKey}", HttpVerb.GET))
            .ReturnsAsync(Result<string>.Ok(newUrl));

        // Act
        await _imageRefreshService.RefreshConversationImagesAsync(conversation);

        // Assert
        Assert.NotNull(conversation.Image);
        Assert.Equal(newUrl, conversation.Image.Url);
    }

    [Fact]
    public async Task RefreshConversationImagesAsync_ShouldRefreshAllParticipantImages()
    {
        // Arrange
        var participant1 = new ApplicationUser
        {
            Id = "user1",
            UserName = "user1",
            ProfileImage = new FileStore(
                "old-url-1",
                DateTime.UtcNow.AddHours(-1),
                "key1.jpg",
                FileCategory.ProfileImage,
                FileType.Image,
                DateTime.UtcNow
            )
        };

        var participant2 = new ApplicationUser
        {
            Id = "user2",
            UserName = "user2",
            ProfileImage = new FileStore(
                "old-url-2",
                DateTime.UtcNow.AddHours(-1),
                "key2.jpg",
                FileCategory.ProfileImage,
                FileType.Image,
                DateTime.UtcNow
            )
        };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            Image = null,
            Participants = new List<ApplicationUser> { participant1, participant2 },
            Creator = null
        };

        _s3ServiceMock
            .Setup(x => x.GetPresignedUrlAsync(It.IsAny<string>(), HttpVerb.GET))
            .ReturnsAsync(Result<string>.Ok("new-url"));

        // Act
        await _imageRefreshService.RefreshConversationImagesAsync(conversation);

        // Assert
        _s3ServiceMock.Verify(
            x => x.GetPresignedUrlAsync($"users/user1/profile-pictures/key1.jpg", HttpVerb.GET),
            Times.Once);
        _s3ServiceMock.Verify(
            x => x.GetPresignedUrlAsync($"users/user2/profile-pictures/key2.jpg", HttpVerb.GET),
            Times.Once);
    }

    [Fact]
    public async Task RefreshConversationImagesAsync_ShouldRefreshCreatorImage()
    {
        // Arrange
        var creator = new ApplicationUser
        {
            Id = "creator1",
            UserName = "creator",
            ProfileImage = new FileStore(
                "old-creator-url",
                DateTime.UtcNow.AddHours(-1),
                "creator-key.jpg",
                FileCategory.ProfileImage,
                FileType.Image,
                DateTime.UtcNow
            )
        };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Team,
            Image = null,
            Participants = new List<ApplicationUser>(),
            Creator = creator
        };

        _s3ServiceMock
            .Setup(x => x.GetPresignedUrlAsync(It.IsAny<string>(), HttpVerb.GET))
            .ReturnsAsync(Result<string>.Ok("new-creator-url"));

        // Act
        await _imageRefreshService.RefreshConversationImagesAsync(conversation);

        // Assert
        _s3ServiceMock.Verify(
            x => x.GetPresignedUrlAsync($"users/creator1/profile-pictures/creator-key.jpg", HttpVerb.GET),
            Times.Once);
    }

    [Fact]
    public async Task RefreshConversationImagesAsync_WithNullCreator_ShouldNotThrow()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            Image = null,
            Participants = new List<ApplicationUser>(),
            Creator = null
        };

        // Act & Assert
        await _imageRefreshService.RefreshConversationImagesAsync(conversation);
        // Should complete without throwing
        Assert.NotNull(conversation);
    }

    [Fact]
    public async Task RefreshConversationImagesAsync_WithCompleteConversation_ShouldRefreshAllImages()
    {
        // Arrange
        var creator = new ApplicationUser
        {
            Id = "creator1",
            UserName = "creator",
            ProfileImage = new FileStore(
                "old-creator-url",
                DateTime.UtcNow.AddHours(-1),
                "creator-key.jpg",
                FileCategory.ProfileImage,
                FileType.Image,
                DateTime.UtcNow
            )
        };

        var participant = new ApplicationUser
        {
            Id = "user1",
            UserName = "user1",
            ProfileImage = new FileStore(
                "old-user-url",
                DateTime.UtcNow.AddHours(-1),
                "user-key.jpg",
                FileCategory.ProfileImage,
                FileType.Image,
                DateTime.UtcNow
            )
        };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Team,
            Image = new FileStore(
                "old-conv-url",
                DateTime.UtcNow.AddHours(-1),
                "conv-key.jpg",
                FileCategory.ConversationImage,
                FileType.Image,
                DateTime.UtcNow
            ),
            Participants = new List<ApplicationUser> { participant },
            Creator = creator
        };

        _s3ServiceMock
            .Setup(x => x.GetPresignedUrlAsync(It.IsAny<string>(), HttpVerb.GET))
            .ReturnsAsync(Result<string>.Ok("new-url"));

        // Act
        await _imageRefreshService.RefreshConversationImagesAsync(conversation);

        // Assert
        // Verify conversation image was refreshed
        _s3ServiceMock.Verify(
            x => x.GetPresignedUrlAsync($"conversations/conv1/image/conv-key.jpg", HttpVerb.GET),
            Times.Once);
        
        // Verify participant image was refreshed
        _s3ServiceMock.Verify(
            x => x.GetPresignedUrlAsync($"users/user1/profile-pictures/user-key.jpg", HttpVerb.GET),
            Times.Once);
        
        // Verify creator image was refreshed
        _s3ServiceMock.Verify(
            x => x.GetPresignedUrlAsync($"users/creator1/profile-pictures/creator-key.jpg", HttpVerb.GET),
            Times.Once);
    }

    #endregion
}
