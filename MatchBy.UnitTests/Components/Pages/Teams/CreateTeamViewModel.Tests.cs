using System.ComponentModel;
using Blazorise;
using FluentValidation;
using FluentValidation.Results;
using MatchBy.Components.Pages.Teams;
using MatchBy.DTOs.Team;
using MatchBy.DTOs.User;
using MatchBy.Models;
using MatchBy.Services.Teams;
using MatchBy.Services.Users;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Moq;

namespace MatchBy.UnitTests.Components.Pages.Teams;

public class CreateTeamViewModelTests
{
    private readonly Mock<ITeamService> _teamServiceMock;
    private readonly Mock<IUsersService> _usersServiceMock;
    private readonly Mock<IValidator<CreateTeamDto>> _validatorMock;
    private readonly Mock<IToastService> _toastServiceMock;
    private readonly CreateTeamViewModel _viewModel;

    public CreateTeamViewModelTests()
    {
        _teamServiceMock = new Mock<ITeamService>();
        _usersServiceMock = new Mock<IUsersService>();
        _validatorMock = new Mock<IValidator<CreateTeamDto>>();
        _toastServiceMock = new Mock<IToastService>();

        _viewModel = new CreateTeamViewModel(
            _teamServiceMock.Object,
            _usersServiceMock.Object,
            _validatorMock.Object,
            _toastServiceMock.Object);
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Assert
        Assert.NotNull(_viewModel.Model);
        Assert.Equal(string.Empty, _viewModel.Model.Name);
        Assert.Equal(string.Empty, _viewModel.Model.Description);
        Assert.Equal(TeamPrivacy.Public, _viewModel.Model.Privacy);
        Assert.Empty(_viewModel.Model.MembersIds);
        Assert.Equal(10, _viewModel.Model.MaxMembers);
        Assert.True(_viewModel.IsLoading);
        Assert.False(_viewModel.IsSubmitting);
        Assert.False(_viewModel.IsLoadingMembers);
        Assert.Equal(string.Empty, _viewModel.MemberSearch);
        Assert.Equal(1, _viewModel.CurrentMemberPage);
        Assert.Null(_viewModel.SelectedImage);
        Assert.Null(_viewModel.UserId);
    }

    #endregion

    #region Property Update Tests

    [Fact]
    public void UpdateName_ShouldUpdateModelName()
    {
        // Arrange
        const string newName = "New Team Name";

        // Act
        _viewModel.UpdateName(newName);

        // Assert
        Assert.Equal(newName, _viewModel.Model.Name);
    }

    [Fact]
    public void UpdateDescription_ShouldUpdateModelDescription()
    {
        // Arrange
        const string newDescription = "New Description";

        // Act
        _viewModel.UpdateDescription(newDescription);

        // Assert
        Assert.Equal(newDescription, _viewModel.Model.Description);
    }

    [Fact]
    public void UpdatePrivacy_ShouldUpdateModelPrivacy()
    {
        // Arrange
        const TeamPrivacy newPrivacy = TeamPrivacy.Private;

        // Act
        _viewModel.UpdatePrivacy(newPrivacy);

        // Assert
        Assert.Equal(newPrivacy, _viewModel.Model.Privacy);
    }

    /*[Fact]
    public void OnImageSelected_ShouldSetSelectedImage()
    {
        // Arrange
        var mockFile = new Mock<IBrowserFile>();
        //var args = new InputFileChangeEventArgs(new[] { mockFile.Object });

        // Act
        //_viewModel.OnImageSelected(args);

        // Assert
        Assert.Equal(mockFile.Object, _viewModel.SelectedImage);
    }*/

    /*[Fact]
    public void RemoveImage_ShouldClearSelectedImage()
    {
        // Arrange
        //var mockFile = new Mock<IBrowserFile>();
        //var args = new InputFileChangeEventArgs(new[] { mockFile.Object });
        //_viewModel.OnImageSelected(args);
        Assert.NotNull(_viewModel.SelectedImage);

        // Act
        _viewModel.RemoveImage();

        // Assert
        Assert.Null(_viewModel.SelectedImage);
    }*/

    #endregion

    #region Member Management Tests

    [Fact]
    public void ToggleMember_WhenMemberNotInList_ShouldAddMember()
    {
        // Arrange
        const string userId = "user1";
        _viewModel.Model = _viewModel.Model with { MembersIds = [] };
        _viewModel.AvailableUsers = new PaginationResponse<List<ApplicationUser>>()
        {
            Data = [],
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        // Act
        _viewModel.ToggleMember(userId);

        // Assert
        Assert.Contains(userId, _viewModel.Model.MembersIds);
        Assert.Single(_viewModel.Model.MembersIds);
    }

    [Fact]
    public void ToggleMember_WhenMemberInList_ShouldRemoveMember()
    {
        // Arrange
        const string userId = "user1";
        _viewModel.Model = _viewModel.Model with { MembersIds = [userId] };
        _viewModel.AvailableUsers = new PaginationResponse<List<ApplicationUser>>()
        {
            Data =
            [
                new ApplicationUser()
                {
                    Id = userId,
                }
            ],
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        // Act
        _viewModel.ToggleMember(userId);

        // Assert
        Assert.DoesNotContain(userId, _viewModel.Model.MembersIds);
        Assert.Empty(_viewModel.Model.MembersIds);
    }

    [Fact]
    public void RemoveMember_WhenMemberExists_ShouldRemoveMember()
    {
        // Arrange
        const string userId = "user1";
        _viewModel.Model = _viewModel.Model with { MembersIds = [userId, "user2"] };
        _viewModel.AvailableUsers = new PaginationResponse<List<ApplicationUser>>()
        {
            Data =
            [
                new ApplicationUser()
                {
                    Id = userId,
                },
                new ApplicationUser()
                {
                    Id = "user2",
                }
            ],
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        // Act
        _viewModel.RemoveMember(userId);

        // Assert
        Assert.DoesNotContain(userId, _viewModel.Model.MembersIds);
        Assert.Contains("user2", _viewModel.Model.MembersIds);
        Assert.Single(_viewModel.Model.MembersIds);
    }

    [Fact]
    public void RemoveMember_WhenMemberDoesNotExist_ShouldNotThrow()
    {
        // Arrange
        const string userId = "user1";
        _viewModel.Model = _viewModel.Model with { MembersIds = ["user2"] };
        _viewModel.AvailableUsers = new PaginationResponse<List<ApplicationUser>>()
        {
            Data = [],
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        // Act & Assert
        _viewModel.RemoveMember(userId);
        Assert.DoesNotContain(userId, _viewModel.Model.MembersIds);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void ValidateProperty_WhenValid_ShouldReturnSuccess()
    {
        // Arrange
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<CreateTeamDto>()))
            .Returns(new ValidationResult());

        // Act
        ValidationStatus result = _viewModel.ValidateProperty(nameof(CreateTeamDto.Name));

        // Assert
        Assert.Equal(ValidationStatus.Success, result);
    }

    [Fact]
    public void ValidateProperty_WhenInvalid_ShouldReturnError()
    {
        // Arrange
        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure(nameof(CreateTeamDto.Name), "Name is required")
        });
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<CreateTeamDto>()))
            .Returns(validationResult);

        // Act
        ValidationStatus result = _viewModel.ValidateProperty(nameof(CreateTeamDto.Name));

        // Assert
        Assert.Equal(ValidationStatus.Error, result);
    }

    [Fact]
    public void GetValidationError_WhenValid_ShouldReturnNull()
    {
        // Arrange
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<CreateTeamDto>()))
            .Returns(new ValidationResult());

        // Act
        string? error = _viewModel.GetValidationError(nameof(CreateTeamDto.Name));

        // Assert
        Assert.Null(error);
    }

    [Fact]
    public void GetValidationError_WhenInvalid_ShouldReturnErrorMessage()
    {
        // Arrange
        const string errorMessage = "Name is required";
        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure(nameof(CreateTeamDto.Name), errorMessage)
        });
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<CreateTeamDto>()))
            .Returns(validationResult);

        // Act
        string? error = _viewModel.GetValidationError(nameof(CreateTeamDto.Name));

        // Assert
        Assert.Equal(errorMessage, error);
    }

    #endregion

    #region LoadMembersAsync Tests

    [Fact]
    public async Task LoadMembersAsync_WhenAlreadyLoading_ShouldReturnEarly()
    {
        // Arrange
        _viewModel.IsLoadingMembers = true;
        int callCount = 0;
        _usersServiceMock
            .Setup(s => s.GetUsers(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback(() => callCount++)
            .ReturnsAsync(
                Result<PaginationResponse<List<ApplicationUser>>>.Ok(new PaginationResponse<List<ApplicationUser>>()));

        // Act
        await _viewModel.LoadMembersAsync();

        // Assert
        Assert.Equal(0, callCount);
    }

    [Fact]
    public async Task LoadMembersAsync_WhenSuccess_ShouldSetAvailableUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "user1", DisplayName = "User 1" },
            new() { Id = "user2", DisplayName = "User 2" }
        };
        var paginationResponse = new PaginationResponse<List<ApplicationUser>>
        {
            Data = users,
            TotalCount = 2,
            Page = 1,
            PageSize = 10
        };

        _usersServiceMock
            .Setup(s => s.GetUsers(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginationResponse<List<ApplicationUser>>>.Ok(paginationResponse));

        // Act
        await _viewModel.LoadMembersAsync();

        // Assert
        Assert.Equal(2, _viewModel.AvailableUsers.Data.Count);
        Assert.False(_viewModel.IsLoadingMembers);
    }

    [Fact]
    public async Task LoadMembersAsync_WhenSuccess_ShouldFilterOutCurrentUser()
    {
        // Arrange
        _viewModel.UserId = "user1";
        var users = new List<ApplicationUser>
        {
            new() { Id = "user1", DisplayName = "User 1" },
            new() { Id = "user2", DisplayName = "User 2" }
        };
        var paginationResponse = new PaginationResponse<List<ApplicationUser>>
        {
            Data = users,
            TotalCount = 2,
            Page = 1,
            PageSize = 10
        };

        _usersServiceMock
            .Setup(s => s.GetUsers(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginationResponse<List<ApplicationUser>>>.Ok(paginationResponse));

        // Act
        await _viewModel.LoadMembersAsync();

        // Assert
        Assert.Single(_viewModel.AvailableUsers.Data);
        Assert.DoesNotContain(_viewModel.AvailableUsers.Data, u => u.Id == "user1");
        Assert.Contains(_viewModel.AvailableUsers.Data, u => u.Id == "user2");
    }

    [Fact]
    public async Task LoadMembersAsync_WhenFailure_ShouldShowErrorToast()
    {
        // Arrange
        var errorResult = Result<PaginationResponse<List<ApplicationUser>>>.Fail("Error loading users");
        _usersServiceMock
            .Setup(s => s.GetUsers(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResult);

        // Act
        await _viewModel.LoadMembersAsync();

        // Assert
        _toastServiceMock.Verify(t => t.Error(It.IsAny<string>(), null, null), Times.Once);
        Assert.False(_viewModel.IsLoadingMembers);
    }

    #endregion

    #region OnMemberSearchKeyDownAsync Tests

    [Fact]
    public async Task OnMemberSearchKeyDownAsync_WhenEnterKey_ShouldLoadMembers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "user1", DisplayName = "User 1" }
        };
        var paginationResponse = new PaginationResponse<List<ApplicationUser>>
        {
            Data = users,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _usersServiceMock
            .Setup(s => s.GetUsers(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginationResponse<List<ApplicationUser>>>.Ok(paginationResponse));

        var keyboardEventArgs = new KeyboardEventArgs { Key = "Enter" };

        // Act
        await _viewModel.OnMemberSearchKeyDownAsync(keyboardEventArgs);

        // Assert
        Assert.Equal(1, _viewModel.CurrentMemberPage);
        _usersServiceMock.Verify(s => s.GetUsers(It.IsAny<string>(), 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnMemberSearchKeyDownAsync_WhenNotEnterKey_ShouldNotLoadMembers()
    {
        // Arrange
        var keyboardEventArgs = new KeyboardEventArgs { Key = "Space" };

        // Act
        await _viewModel.OnMemberSearchKeyDownAsync(keyboardEventArgs);

        // Assert
        _usersServiceMock.Verify(
            s => s.GetUsers(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region OnMemberPageChangedAsync Tests

    [Fact]
    public async Task OnMemberPageChangedAsync_ShouldUpdatePageAndLoadMembers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "user1", DisplayName = "User 1" }
        };
        var paginationResponse = new PaginationResponse<List<ApplicationUser>>
        {
            Data = users,
            TotalCount = 1,
            Page = 2,
            PageSize = 10
        };

        _usersServiceMock
            .Setup(s => s.GetUsers(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginationResponse<List<ApplicationUser>>>.Ok(paginationResponse));

        // Act
        await _viewModel.OnMemberPageChangedAsync(2);

        // Assert
        Assert.Equal(2, _viewModel.CurrentMemberPage);
        _usersServiceMock.Verify(s => s.GetUsers(It.IsAny<string>(), 2, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SubmitTeamAsync Tests

    [Fact]
    public async Task SubmitTeamAsync_WhenSuccess_ShouldReturnTrue()
    {
        // Arrange
        _viewModel.UserId = "user1";
        _viewModel.Model = _viewModel.Model with
        {
            Name = "Test Team",
            Description = "Test Description",
            MembersIds = new List<string> { "user2" }
        };

        var teamDto = new TeamDto
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<UserDto>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _teamServiceMock
            .Setup(s => s.CreateTeamAsync(It.IsAny<CreateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TeamDto>.Ok(teamDto));

        // Act
        bool result = await _viewModel.SubmitTeamAsync();

        // Assert
        Assert.True(result);
        Assert.False(_viewModel.IsSubmitting);
        _teamServiceMock.Verify(
            s => s.CreateTeamAsync(
                It.Is<CreateTeamDto>(d =>
                    d.OwnerId == "user1" && d.MembersIds.Contains("user1") && d.MembersIds.Contains("user2")),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitTeamAsync_WhenSuccessWithMembers_ShouldShowSuccessToastWithInviteCount()
    {
        // Arrange
        _viewModel.UserId = "user1";
        _viewModel.Model = _viewModel.Model with
        {
            Name = "Test Team",
            Description = "Test Description",
            MembersIds = new List<string> { "user2", "user3" }
        };

        var teamDto = new TeamDto
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<UserDto>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _teamServiceMock
            .Setup(s => s.CreateTeamAsync(It.IsAny<CreateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TeamDto>.Ok(teamDto));

        // Act
        await _viewModel.SubmitTeamAsync();

        // Assert
        _toastServiceMock.Verify(t => t.Success(It.Is<string>(s => s.Contains("2 user(s)")), "Create Team", null),
            Times.Once);
    }

    [Fact]
    public async Task SubmitTeamAsync_WhenSuccessWithoutMembers_ShouldShowSuccessToast()
    {
        // Arrange
        _viewModel.UserId = "user1";
        _viewModel.Model = _viewModel.Model with
        {
            Name = "Test Team",
            Description = "Test Description",
            MembersIds = new List<string>()
        };

        var teamDto = new TeamDto
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<UserDto>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _teamServiceMock
            .Setup(s => s.CreateTeamAsync(It.IsAny<CreateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TeamDto>.Ok(teamDto));

        // Act
        await _viewModel.SubmitTeamAsync();

        // Assert
        _toastServiceMock.Verify(t => t.Success("Team created successfully!", "Create Team", null), Times.Once);
    }

    [Fact]
    public async Task SubmitTeamAsync_WhenFailure_ShouldReturnFalseAndShowError()
    {
        // Arrange
        _viewModel.UserId = "user1";
        _viewModel.Model = _viewModel.Model with
        {
            Name = "Test Team",
            Description = "Test Description"
        };

        var errorResult = Result<TeamDto>.Fail("Error creating team");
        _teamServiceMock
            .Setup(s => s.CreateTeamAsync(It.IsAny<CreateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResult);

        // Act
        bool result = await _viewModel.SubmitTeamAsync();

        // Assert
        Assert.False(result);
        Assert.False(_viewModel.IsSubmitting);
        _toastServiceMock.Verify(t => t.Error("Error creating team", null, null), Times.Once);
    }

    /*[Fact]
    public async Task SubmitTeamAsync_WhenFailureWithNoErrorMessage_ShouldShowDefaultError()
    {
        // Arrange
        _viewModel.UserId = "user1";
        _viewModel.Model = _viewModel.Model with
        {
            Name = "Test Team",
            Description = "Test Description"
        };

        var errorResult = Result<TeamDto>.Fail();
        _teamServiceMock
            .Setup(s => s.CreateTeamAsync(It.IsAny<CreateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResult);

        // Act
        bool result = await _viewModel.SubmitTeamAsync();

        // Assert
        Assert.False(result);
        _toastServiceMock.Verify(t => t.Error("Error creating team.", null, null), Times.Once);
    }*/

    /*[Fact]
    public async Task SubmitTeamAsync_ShouldIncludeSelectedImageInDto()
    {
        // Arrange
        _viewModel.UserId = "user1";
        var mockFile = new Mock<IBrowserFile>();
        //var args = new InputFileChangeEventArgs([mockFile.Object]);
        //_viewModel.OnImageSelected(args);

        var teamDto = new TeamDto
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<UserDto>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _teamServiceMock
            .Setup(s => s.CreateTeamAsync(It.IsAny<CreateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TeamDto>.Ok(teamDto));

        // Act
        await _viewModel.SubmitTeamAsync();

        // Assert
        _teamServiceMock.Verify(
            s => s.CreateTeamAsync(It.Is<CreateTeamDto>(d => d.File == mockFile.Object), It.IsAny<CancellationToken>()),
            Times.Once);
    }*/

    [Fact]
    public async Task SubmitTeamAsync_ShouldAddOwnerToMembersIds()
    {
        // Arrange
        _viewModel.UserId = "user1";
        _viewModel.Model = _viewModel.Model with
        {
            Name = "Test Team",
            Description = "Test Description",
            MembersIds = new List<string> { "user2" }
        };

        var teamDto = new TeamDto
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<UserDto>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _teamServiceMock
            .Setup(s => s.CreateTeamAsync(It.IsAny<CreateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TeamDto>.Ok(teamDto));

        // Act
        await _viewModel.SubmitTeamAsync();

        // Assert
        _teamServiceMock.Verify(s => s.CreateTeamAsync(It.Is<CreateTeamDto>(d =>
            d.OwnerId == "user1" &&
            d.MembersIds.Contains("user1") &&
            d.MembersIds.Contains("user2") &&
            d.MembersIds.Count == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitTeamAsync_ShouldRemoveDuplicateMembers()
    {
        // Arrange
        _viewModel.UserId = "user1";
        _viewModel.Model = _viewModel.Model with
        {
            Name = "Test Team",
            Description = "Test Description",
            MembersIds = new List<string> { "user1", "user2", "user1" } // user1 appears twice
        };

        var teamDto = new TeamDto
        {
            Id = "team1",
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "user1",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            Members = new List<UserDto>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _teamServiceMock
            .Setup(s => s.CreateTeamAsync(It.IsAny<CreateTeamDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TeamDto>.Ok(teamDto));

        // Act
        await _viewModel.SubmitTeamAsync();

        // Assert
        _teamServiceMock.Verify(s => s.CreateTeamAsync(It.Is<CreateTeamDto>(d =>
            d.MembersIds.Distinct().Count() == d.MembersIds.Count), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region PropertyChanged Tests

    [Fact]
    public void PropertyChanged_WhenPropertyChanges_ShouldRaiseEvent()
    {
        // Arrange
        PropertyChangedEventArgs? capturedArgs = null;
        _viewModel.PropertyChanged += (_, e) => capturedArgs = e;

        // Act
        _viewModel.IsLoading = false;

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal(nameof(CreateTeamViewModel.IsLoading), capturedArgs.PropertyName);
    }

    #endregion
}