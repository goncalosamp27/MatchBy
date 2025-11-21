using FluentValidation.TestHelper;
using MatchBy.DTOs.Team;
using MatchBy.Models;
using MatchBy.Services.FileValidator;
using Moq;

namespace MatchBy.UnitTests.DTOs.Team;

public class UpdateTeamDtoValidatorTests
{
    private readonly Mock<IFileValidator> _fileValidatorMock = new();
    private readonly UpdateTeamDtoValidator _validator;

    public UpdateTeamDtoValidatorTests()
    {
        _fileValidatorMock.Setup(x => x.GetMaxFileBytes()).Returns(5 * 1024 * 1024); // 5 MB
        _validator = new UpdateTeamDtoValidator(_fileValidatorMock.Object);
    }

    private UpdateTeamDto CreateValidDto()
    {
        return new UpdateTeamDto
        {
            Id = "team_123",
            Name = "Team Alpha",
            Description = "Test Description",
            OwnerId = "owner_123",
            MaxMembers = 10,
            Privacy = TeamPrivacy.Public,
            MembersIds = new List<string> { "owner_123", "user_456" },
            File = null
        };
    }

    #region Id Validation Tests

    [Fact]
    public void Validate_IdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        UpdateTeamDto dto = CreateValidDto() with { Id = string.Empty };

        // Act
        TestValidationResult<UpdateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("TeamId is required.");
    }

    [Fact]
    public void Validate_IdIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateTeamDto dto = CreateValidDto();

        // Act
        TestValidationResult<UpdateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    #endregion

    #region Name Validation Tests

    [Fact]
    public void Validate_NameIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        UpdateTeamDto dto = CreateValidDto() with { Name = string.Empty };

        // Act
        TestValidationResult<UpdateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name is required.");
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        UpdateTeamDto dto = CreateValidDto() with { Name = new string('a', 501) };

        // Act
        TestValidationResult<UpdateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name cannot exceed 500 characters.");
    }

    #endregion

    #region Description Validation Tests

    [Fact]
    public void Validate_DescriptionIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        UpdateTeamDto dto = CreateValidDto() with { Description = string.Empty };

        // Act
        TestValidationResult<UpdateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description is required.");
    }

    [Fact]
    public void Validate_DescriptionExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        UpdateTeamDto dto = CreateValidDto() with { Description = new string('a', 501) };

        // Act
        TestValidationResult<UpdateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description cannot exceed 500 characters.");
    }

    #endregion

    #region OwnerId Validation Tests

    [Fact]
    public void Validate_OwnerIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        UpdateTeamDto dto = CreateValidDto() with { OwnerId = string.Empty, MembersIds = new List<string> { "user_456" } };

        // Act
        TestValidationResult<UpdateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OwnerId)
              .WithErrorMessage("OwnerId is required.");
    }

    #endregion

    #region Privacy Validation Tests

    [Fact]
    public void Validate_PrivacyIsInvalidEnum_ShouldHaveValidationError()
    {
        // Arrange
        UpdateTeamDto dto = CreateValidDto() with { Privacy = (TeamPrivacy)999 };

        // Act
        TestValidationResult<UpdateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Privacy)
              .WithErrorMessage("Privacy must be a valid value.");
    }

    #endregion

    #region MembersIds Validation Tests

    [Fact]
    public void Validate_MembersIdsIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        UpdateTeamDto dto = CreateValidDto() with { MembersIds = new List<string>() };

        // Act
        TestValidationResult<UpdateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MembersIds)
              .WithErrorMessage("Provide at least one member.");
    }

    [Fact]
    public void Validate_MembersIdsContainsEmptyString_ShouldHaveValidationError()
    {
        // Arrange
        UpdateTeamDto dto = CreateValidDto() with { MembersIds = new List<string> { "owner_123", "" } };

        // Act
        TestValidationResult<UpdateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MembersIds)
              .WithErrorMessage("Some member IDs are empty.");
    }

    [Fact]
    public void Validate_MembersIdsContainsDuplicates_ShouldHaveValidationError()
    {
        // Arrange
        UpdateTeamDto dto = CreateValidDto() with { MembersIds = new List<string> { "owner_123", "user_456", "owner_123" } };

        // Act
        TestValidationResult<UpdateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MembersIds)
              .WithErrorMessage("Duplicate member IDs are not allowed.");
    }

    [Fact]
    public void Validate_MembersIdsDoesNotContainOwner_ShouldHaveValidationError()
    {
        // Arrange
        UpdateTeamDto dto = CreateValidDto() with
        {
            OwnerId = "owner_123",
            MembersIds = new List<string> { "user_456", "user_789" }
        };

        // Act
        TestValidationResult<UpdateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Creator must be included in MembersIds.");
    }

    #endregion

    #region File Validation Tests

    [Fact]
    public void Validate_FileIsNull_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateTeamDto dto = CreateValidDto() with { File = null };

        // Act
        TestValidationResult<UpdateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.File);
    }

    [Fact]
    public void Validate_FileIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        Mock<Microsoft.AspNetCore.Components.Forms.IBrowserFile> fileMock = new();
        _fileValidatorMock.Setup(x => x.IsValidBrowserImage(It.IsAny<Microsoft.AspNetCore.Components.Forms.IBrowserFile>()))
            .Returns(true);

        UpdateTeamDto dto = CreateValidDto() with { File = fileMock.Object };

        // Act
        TestValidationResult<UpdateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.File);
    }

    [Fact]
    public void Validate_FileIsInvalid_ShouldHaveValidationError()
    {
        // Arrange
        Mock<Microsoft.AspNetCore.Components.Forms.IBrowserFile> fileMock = new();
        _fileValidatorMock.Setup(x => x.IsValidBrowserImage(It.IsAny<Microsoft.AspNetCore.Components.Forms.IBrowserFile>()))
            .Returns(false);

        UpdateTeamDto dto = CreateValidDto() with { File = fileMock.Object };

        // Act
        TestValidationResult<UpdateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.File!);
    }

    #endregion

    #region Complete DTO Validation Tests

    [Fact]
    public void Validate_AllFieldsValid_ShouldPass()
    {
        // Arrange
        UpdateTeamDto dto = CreateValidDto();

        // Act
        TestValidationResult<UpdateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

