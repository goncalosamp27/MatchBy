using FluentValidation.TestHelper;
using MatchBy.DTOs.Contact;

namespace MatchBy.UnitTests.DTOs.Contact;

public class ContactFormDtoValidatorTests
{
    private readonly ContactFormDtoValidator _validator = new();

    #region Name Validation Tests

    [Fact]
    public void Validate_NameIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        ContactFormDto dto = new()
        {
            Name = string.Empty,
            Email = "test@example.com",
            Subject = "Test Subject",
            Message = "Test Message"
        };

        // Act
        FluentValidation.TestHelper.TestValidationResult<ContactFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name is required.");
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        ContactFormDto dto = new()
        {
            Name = new string('a', 101), // 101 characters
            Email = "test@example.com",
            Subject = "Test Subject",
            Message = "Test Message"
        };

        // Act
        FluentValidation.TestHelper.TestValidationResult<ContactFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_NameIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        ContactFormDto dto = new()
        {
            Name = "John Doe",
            Email = "test@example.com",
            Subject = "Test Subject",
            Message = "Test Message"
        };

        // Act
        FluentValidation.TestHelper.TestValidationResult<ContactFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Email Validation Tests

    [Fact]
    public void Validate_EmailIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        ContactFormDto dto = new()
        {
            Name = "John Doe",
            Email = string.Empty,
            Subject = "Test Subject",
            Message = "Test Message"
        };

        // Act
        FluentValidation.TestHelper.TestValidationResult<ContactFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email is required.");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    public void Validate_EmailIsInvalidFormat_ShouldHaveValidationError(string invalidEmail)
    {
        // Arrange
        ContactFormDto dto = new()
        {
            Name = "John Doe",
            Email = invalidEmail,
            Subject = "Test Subject",
            Message = "Test Message"
        };

        // Act
        TestValidationResult<ContactFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Please enter a valid email address.");
    }

    [Fact]
    public void Validate_EmailExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        ContactFormDto dto = new()
        {
            Name = "John Doe",
            Email = new string('a', 190) + "@example.com", // 201 characters
            Subject = "Test Subject",
            Message = "Test Message"
        };

        // Act
        FluentValidation.TestHelper.TestValidationResult<ContactFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email cannot exceed 200 characters.");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("john.doe@subdomain.example.com")]
    [InlineData("user+tag@example.co.uk")]
    public void Validate_EmailIsValid_ShouldNotHaveValidationError(string validEmail)
    {
        // Arrange
        ContactFormDto dto = new()
        {
            Name = "John Doe",
            Email = validEmail,
            Subject = "Test Subject",
            Message = "Test Message"
        };

        // Act
        FluentValidation.TestHelper.TestValidationResult<ContactFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region Subject Validation Tests

    [Fact]
    public void Validate_SubjectIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        ContactFormDto dto = new()
        {
            Name = "John Doe",
            Email = "test@example.com",
            Subject = string.Empty,
            Message = "Test Message"
        };

        // Act
        FluentValidation.TestHelper.TestValidationResult<ContactFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Subject)
              .WithErrorMessage("Subject is required.");
    }

    [Fact]
    public void Validate_SubjectExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        ContactFormDto dto = new()
        {
            Name = "John Doe",
            Email = "test@example.com",
            Subject = new string('a', 201), // 201 characters
            Message = "Test Message"
        };

        // Act
        FluentValidation.TestHelper.TestValidationResult<ContactFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Subject)
              .WithErrorMessage("Subject cannot exceed 200 characters.");
    }

    [Fact]
    public void Validate_SubjectIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        ContactFormDto dto = new()
        {
            Name = "John Doe",
            Email = "test@example.com",
            Subject = "Valid Subject",
            Message = "Test Message"
        };

        // Act
        FluentValidation.TestHelper.TestValidationResult<ContactFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Subject);
    }

    #endregion

    #region Message Validation Tests

    [Fact]
    public void Validate_MessageIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        ContactFormDto dto = new()
        {
            Name = "John Doe",
            Email = "test@example.com",
            Subject = "Test Subject",
            Message = string.Empty
        };

        // Act
        FluentValidation.TestHelper.TestValidationResult<ContactFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Message)
              .WithErrorMessage("Message is required.");
    }

    [Fact]
    public void Validate_MessageExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        ContactFormDto dto = new()
        {
            Name = "John Doe",
            Email = "test@example.com",
            Subject = "Test Subject",
            Message = new string('a', 2001) // 2001 characters
        };

        // Act
        FluentValidation.TestHelper.TestValidationResult<ContactFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Message)
              .WithErrorMessage("Message cannot exceed 2000 characters.");
    }

    [Fact]
    public void Validate_MessageIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        ContactFormDto dto = new()
        {
            Name = "John Doe",
            Email = "test@example.com",
            Subject = "Test Subject",
            Message = "This is a valid message."
        };

        // Act
        FluentValidation.TestHelper.TestValidationResult<ContactFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Message);
    }

    #endregion

    #region Complete DTO Validation Tests

    [Fact]
    public void Validate_AllFieldsValid_ShouldPass()
    {
        // Arrange
        ContactFormDto dto = new()
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            Subject = "Important Question",
            Message = "I have a question about your service."
        };

        // Act
        FluentValidation.TestHelper.TestValidationResult<ContactFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_AllFieldsInvalid_ShouldHaveMultipleErrors()
    {
        // Arrange
        ContactFormDto dto = new()
        {
            Name = string.Empty,
            Email = "invalid-email",
            Subject = string.Empty,
            Message = string.Empty
        };

        // Act
        FluentValidation.TestHelper.TestValidationResult<ContactFormDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Subject);
        result.ShouldHaveValidationErrorFor(x => x.Message);
    }

    #endregion
}

