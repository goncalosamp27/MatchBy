using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

public class RegisterPageTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string RegisterUrl = BaseUrl + "/Account/Register";
    
    
  

    [Fact]
    public async Task RegisterPage_ShouldRegisterSuccessfullyWithValidData()
    {
        (string email, string username, string password, string displayName) = GenerateUser();
        try
        {
            
            await Page.GotoAsync(RegisterUrl);

            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync(email);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).PressAsync("Tab");
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Username" }).FillAsync(username);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Username" }).PressAsync("Tab");
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Display Name" }).FillAsync(displayName);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Display Name" }).PressAsync("Tab");
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password", Exact = true }).FillAsync(password);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password", Exact = true }).PressAsync("Tab");
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Confirm Password" }).FillAsync(password);
            await Page.GetByRole(AriaRole.Checkbox, new() { Name = "I accept the Terms and" }).CheckAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();
            await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Confirmation email sent" })).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions()
            {
                Timeout = 30000
            });
        }
        finally
        {
            await Page.CloseAsync();
        }


    }

    [Fact]
    public async Task RegisterPage_ShouldShowErrorWithExistingEmail()
    {
        // Arrange - Generate a unique user
        (string email, string username, string password, string displayName) = GenerateUser();

        try
        {
            // Step 1: Register the user for the first time (should succeed)
            await Page.GotoAsync(RegisterUrl);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync(email);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Username" }).FillAsync(username);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Display Name" }).FillAsync(displayName);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password", Exact = true }).FillAsync(password);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Confirm Password" }).FillAsync(password);
            await Page.GetByRole(AriaRole.Checkbox, new() { Name = "I accept the Terms and" }).CheckAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();
            
            // Wait for confirmation that first registration succeeded
            await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Confirmation email sent" }))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions() { Timeout = 10000 });

            // Step 2: Try to register again with the SAME email but different username (should fail)
            await Page.GotoAsync(RegisterUrl);
            (string _, string newUsername, string newPassword, string newDisplayName) = GenerateUser();

            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync(email); // Same email as before
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Username" }).FillAsync(newUsername); // Different username
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Display Name" }).FillAsync(newDisplayName);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password", Exact = true }).FillAsync(newPassword);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Confirm Password" }).FillAsync(newPassword);
            await Page.GetByRole(AriaRole.Checkbox, new() { Name = "I accept the Terms and" }).CheckAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();

            // Assert - Should see error about duplicate email
            await Expect(Page.GetByText("Error: Email address is")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions()
            {
                Timeout = 30000
            });
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task RegisterPage_ShouldShowErrorWithExistingUsername()
    {
        // Arrange - Generate a unique user
        (string email, string username, string password, string displayName) = GenerateUser();

        try
        {
            // Step 1: Register the user for the first time (should succeed)
            await Page.GotoAsync(RegisterUrl);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync(email);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Username" }).FillAsync(username);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Display Name" }).FillAsync(displayName);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password", Exact = true }).FillAsync(password);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Confirm Password" }).FillAsync(password);
            await Page.GetByRole(AriaRole.Checkbox, new() { Name = "I accept the Terms and" }).CheckAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();
            
            // Wait for confirmation that first registration succeeded
            await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Confirmation email sent" }))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions() { Timeout = 10000 });

            // Step 2: Try to register again with the SAME username but different email (should fail)
            await Page.GotoAsync(RegisterUrl);
            (string newEmail, string _, string newPassword, string newDisplayName) = GenerateUser();

            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync(newEmail); // Different email
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Username" }).FillAsync(username); // Same username as before
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Display Name" }).FillAsync(newDisplayName);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password", Exact = true }).FillAsync(newPassword);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Confirm Password" }).FillAsync(newPassword);
            await Page.GetByRole(AriaRole.Checkbox, new() { Name = "I accept the Terms and" }).CheckAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();

            // Assert - Should see error about duplicate username
            await Expect(Page.GetByText("Error: Username")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions()
            {
                Timeout = 30000
            });
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task RegisterPage_ShouldShowErrorWithPasswordMismatch()
    {
        // Arrange - Generate a unique user
        (string email, string username, string password, string displayName) = GenerateUser();

        try
        {
            await Page.GotoAsync(RegisterUrl);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync(email);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Username" }).FillAsync(username);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Display Name" }).FillAsync(displayName);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password", Exact = true }).FillAsync(password);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Confirm Password" }).FillAsync("DifferentPassword123!"); // Mismatched password
            await Page.GetByRole(AriaRole.Checkbox, new() { Name = "I accept the Terms and" }).CheckAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();

            // Assert - Should see error about password mismatch
            await Expect(Page.Locator("text=/password.*confirmation.*do not match/i"))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions()
                {
                    Timeout = 30000
                });
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task RegisterPage_ShouldShowErrorWithWeakPassword()
    {
        // Arrange - Generate a unique user
        (string email, string username, string _, string displayName) = GenerateUser();
        string weakPassword = "weak"; // Password not strong enough

        try
        {
            await Page.GotoAsync(RegisterUrl);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync(email);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Username" }).FillAsync(username);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Display Name" }).FillAsync(displayName);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password", Exact = true }).FillAsync(weakPassword);
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Confirm Password" }).FillAsync(weakPassword);
            await Page.GetByRole(AriaRole.Checkbox, new() { Name = "I accept the Terms and" }).CheckAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();

            // Assert - Should see error about password strength requirements
            // Common password validation messages include: must be at least X characters, must contain uppercase, etc.
            await Expect(Page.Locator("text=/.*password.*must.*|.*Password.*required.*/i").First)
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions()
                {
                    Timeout = 30000
                });
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task RegisterPage_ShouldShowErrorsWithNoData()
    {
        try
        {
            await Page.GotoAsync(RegisterUrl);
            await Page.GetByRole(AriaRole.Heading, new() { Name = "Create an account" }).ClickAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();
            await Expect(Page.GetByText("The Email field is required.")).ToBeVisibleAsync();
            await Expect(Page.GetByText("The Username field is")).ToBeVisibleAsync();
            await Expect(Page.GetByText("The DisplayName field is")).ToBeVisibleAsync();
            await Expect(Page.GetByText("The Password field is")).ToBeVisibleAsync();
            await Expect(Page.GetByText("The Confirm password field is")).ToBeVisibleAsync();
            await Expect(Page.GetByText("You must accept the terms and")).ToBeVisibleAsync();


        }
        finally
        {
            await Page.CloseAsync();
        }
    }
    


    private (string Email, string Username, string Password, string DisplayName) GenerateUser()
    {
        String unique = Guid.NewGuid().ToString("N").Substring(0, 8);

        String email = $"user_{unique}@test.com";
        String username = $"user_{unique}";
        String displayName = $"User {unique}";
        String password = "Test_1aA!";

        return (email, username, password, displayName);
    }
    
}
