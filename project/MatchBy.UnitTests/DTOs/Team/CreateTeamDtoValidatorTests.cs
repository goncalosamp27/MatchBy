using FluentValidation.TestHelper;
using MatchBy.DTOs.Team;
using MatchBy.Models;
using MatchBy.Services.FileValidator;
using Moq;

namespace MatchBy.UnitTests.DTOs.Team;

public class CreateTeamDtoValidatorTests
{
    private readonly Mock<IFileValidator> _fileValidatorMock = new();
    private readonly CreateTeamDtoValidator _validator;

    public CreateTeamDtoValidatorTests()
    {
        _fileValidatorMock.Setup(x => x.GetMaxFileBytes()).Returns(5 * 1024 * 1024); // 5 MB
        _validator = new CreateTeamDtoValidator(_fileValidatorMock.Object);
    }

    private static CreateTeamDto CreateValidDto()
    {
        return new CreateTeamDto
        {
            Name = "Team Alpha",
            Description = "Test Description",
            OwnerId = "owner_123",
            MaxMembers = 10,
            Privacy = TeamPrivacy.Public,
            MembersIds = new List<string> { "owner_123", "user_456" },
            File = null
        };
    }

    #region Name Validation Tests

    [Fact]
    public void Validate_NameIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        CreateTeamDto dto = CreateValidDto() with { Name = string.Empty };

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name is required.");
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        CreateTeamDto dto = CreateValidDto() with { Name = new string('a', 501) };

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_NameIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateTeamDto dto = CreateValidDto();

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Description Validation Tests

    [Fact]
    public void Validate_DescriptionIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        CreateTeamDto dto = CreateValidDto() with { Description = string.Empty };

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description is required.");
    }

    [Fact]
    public void Validate_DescriptionExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        CreateTeamDto dto = CreateValidDto() with { Description = new string('a', 501) };

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_DescriptionIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateTeamDto dto = CreateValidDto();

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    #endregion

    #region MaxMembers Validation Tests

    [Fact]
    public void Validate_MaxMembersIsOne_ShouldHaveValidationError()
    {
        // Arrange
        CreateTeamDto dto = CreateValidDto() with { MaxMembers = 1 };

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxMembers)
              .WithErrorMessage("MaxMembers must be greater than 1.");
    }

    [Fact]
    public void Validate_MaxMembersExceeds100_ShouldHaveValidationError()
    {
        // Arrange
        CreateTeamDto dto = CreateValidDto() with { MaxMembers = 101 };

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxMembers)
              .WithErrorMessage("MaxMembers cannot exceed 100.");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_MaxMembersIsValid_ShouldNotHaveValidationError(int maxMembers)
    {
        // Arrange
        CreateTeamDto dto = CreateValidDto() with { MaxMembers = maxMembers };

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MaxMembers);
    }

    #endregion

    #region OwnerId Validation Tests

    [Fact]
    public void Validate_OwnerIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        CreateTeamDto dto = CreateValidDto() with { OwnerId = string.Empty, MembersIds = new List<string> { "user_456" } };

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OwnerId)
              .WithErrorMessage("OwnerId is required.");
    }

    [Fact]
    public void Validate_OwnerIdIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateTeamDto dto = CreateValidDto();

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.OwnerId);
    }

    #endregion

    #region Privacy Validation Tests

    [Fact]
    public void Validate_PrivacyIsValidEnum_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateTeamDto dto1 = CreateValidDto() with { Privacy = TeamPrivacy.Public };
        CreateTeamDto dto2 = CreateValidDto() with { Privacy = TeamPrivacy.Private };

        // Act
        TestValidationResult<CreateTeamDto>? result1 = _validator.TestValidate(dto1);
        TestValidationResult<CreateTeamDto>? result2 = _validator.TestValidate(dto2);

        // Assert
        result1.ShouldNotHaveValidationErrorFor(x => x.Privacy);
        result2.ShouldNotHaveValidationErrorFor(x => x.Privacy);
    }

    [Fact]
    public void Validate_PrivacyIsInvalidEnum_ShouldHaveValidationError()
    {
        // Arrange
        CreateTeamDto dto = CreateValidDto() with { Privacy = (TeamPrivacy)999 };

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

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
        CreateTeamDto dto = CreateValidDto() with { MembersIds = new List<string>() };

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MembersIds)
              .WithErrorMessage("Provide at least one member.");
    }

    [Fact]
    public void Validate_MembersIdsContainsEmptyString_ShouldHaveValidationError()
    {
        // Arrange
        CreateTeamDto dto = CreateValidDto() with { MembersIds = new List<string> { "owner_123", "" } };

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MembersIds)
              .WithErrorMessage("Some member IDs are empty.");
    }

    [Fact]
    public void Validate_MembersIdsContainsDuplicates_ShouldHaveValidationError()
    {
        // Arrange
        CreateTeamDto dto = CreateValidDto() with { MembersIds = new List<string> { "owner_123", "user_456", "owner_123" } };

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MembersIds)
              .WithErrorMessage("Duplicate member IDs are not allowed.");
    }

    [Fact]
    public void Validate_MembersIdsDoesNotContainOwner_ShouldHaveValidationError()
    {
        // Arrange
        CreateTeamDto dto = CreateValidDto() with
        {
            OwnerId = "owner_123",
            MembersIds = new List<string> { "user_456", "user_789" }
        };

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Creator must be included in MembersIds.");
    }

    [Fact]
    public void Validate_MembersIdsIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateTeamDto dto = CreateValidDto();

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MembersIds);
    }

    #endregion

    #region File Validation Tests

    [Fact]
    public void Validate_FileIsNull_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateTeamDto dto = CreateValidDto() with { File = null };

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

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

        CreateTeamDto dto = CreateValidDto() with { File = fileMock.Object };

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

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

        CreateTeamDto dto = CreateValidDto() with { File = fileMock.Object };

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.File!);
    }

    #endregion

    #region Complete DTO Validation Tests

    [Fact]
    public void Validate_AllFieldsValid_ShouldPass()
    {
        // Arrange
        CreateTeamDto dto = CreateValidDto();

        // Act
        TestValidationResult<CreateTeamDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

