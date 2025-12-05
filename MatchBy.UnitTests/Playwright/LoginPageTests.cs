using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

public class LoginPageTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string LoginUrl = BaseUrl + "/Account/Login";
    private const string ValidEmail = "admin@admin.com";
    private const string ValidPassword = "Admin!123";
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
    public async Task LoginPage_ShouldLoginSuccessfullyWithAdminCredentials()
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
            await Expect(Page.GetByText("admin@admin.com").First).ToBeVisibleAsync();


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
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Your email" }).FillAsync("notanemail");
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Password" }).FillAsync(ValidPassword);
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Sign in to your account" }).ClickAsync();

            // Assert - Should see validation error for invalid email format
            await Expect(Page.Locator("text=/Email field is not a valid|Email.*not.*valid|Invalid.*email/i").First)
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
}
