using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

public partial class LoginPageTests : PageTest
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
            Assert.Equal("Welcome back", await heading.TextContentAsync());

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
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open user menu user photo" })
                .ClickAsync();
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "My Profile" })
                .ClickAsync();
            await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = ValidEmail })).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions()
            {
                Timeout = 30000
            });
            await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Preferred Sports" })).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions()
            {
                Timeout = 30000
            });
            await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Create Matches" })).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions()
            {
                Timeout = 30000
            });
            await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Previous Matches" })).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions()
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
}