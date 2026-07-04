using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

public class LoginPageTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string LoginUrl = BaseUrl + "/Account/Login";
    private const string ValidEmail = "test1@test.com";
    private const string ValidPassword = "Test!123";
    private const string InvalidPassword = ValidPassword + "blablabla";

    [Fact]
    public async Task LoginPage_ShouldLoadSuccessfully()
    {
        try
        {
            await Page.GotoAsync(LoginUrl);

            string title = await Page.TitleAsync();
            Assert.Contains("MatchBy", title);

            ILocator heading = Page.Locator("h1").First;
            await Assertions.Expect(heading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions()
            {
                Timeout = 30000
            });

            string? bodyText = await Page.TextContentAsync("body");
            Assert.NotNull(bodyText);
            Assert.Contains("Sign in", bodyText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task LoginPage_ShouldLoginSuccessfullyWithValidCredentials()
    {
        try
        {
            await Page.GotoAsync(LoginUrl);

            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Log in" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Your email" })
                .FillAsync(ValidEmail);
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Password" })
                .FillAsync(ValidPassword);
            await Page.GetByRole(AriaRole.Checkbox, new PageGetByRoleOptions { Name = "Remember me" })
                .CheckAsync();
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Sign in to your account" })
                .ClickAsync();
            
            // Wait for navigation after successful login (redirects to home page)
            await Page.WaitForURLAsync("**/", new PageWaitForURLOptions { Timeout = 40000 });
            
            // Wait for user menu button to be visible after login
            ILocator userMenuButton = Page.GetByRole(AriaRole.Button, new() { Name = "Open user menu user photo" });
            await Expect(userMenuButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });
            
            // Click user menu and verify email is displayed
            await userMenuButton.ClickAsync();
            await Expect(Page.GetByText(ValidEmail).First).ToBeVisibleAsync();


        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task LoginPage_ShouldNotLoginWithInvalidCredentials()
    {
        try
        {
            await Page.GotoAsync(LoginUrl);
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Log in" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Your email" }).FillAsync(ValidEmail);
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Password" }).FillAsync(InvalidPassword);
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Sign in to your account" }).ClickAsync();
            
            await Expect(Page.GetByText("Error: Invalid login attempt.")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions()
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
    public async Task LoginPage_ShouldShowErrorWithInvalidEmailFormat()
    {
        try
        {
            await Page.GotoAsync(LoginUrl);
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Log in" }).ClickAsync();

            // Fill with invalid email format (missing @, no domain, etc.)
            ILocator emailInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Your email" });
            await emailInput.FillAsync("notanemail");
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Password" }).FillAsync(ValidPassword);
            
            // Click sign in button
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Sign in to your account" }).ClickAsync();
            
            // Wait a moment for any validation to appear
            await Task.Delay(2000);

            // Assert - Check that we're still on the login page (didn't navigate away due to validation error)
            // OR check for HTML5 validation message
            string currentUrl = Page.Url;
            Assert.Contains("/Account/Login", currentUrl);
            
            // Optionally check if the email field has validation state
            // HTML5 email validation should prevent form submission with invalid email
            string? validationMessage = await emailInput.EvaluateAsync<string?>("el => el.validationMessage");
            
            // If there's a validation message or we're still on login page, the validation worked
            bool isStillOnLoginPage = currentUrl.Contains("/Account/Login");
            Assert.True(isStillOnLoginPage || !string.IsNullOrEmpty(validationMessage), 
                "Expected to remain on login page or show validation message with invalid email format");
        }
        finally
        {
            await Page.CloseAsync();
        }
    }
}
