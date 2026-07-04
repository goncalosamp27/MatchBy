using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

public class ContactTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string LoginUrl = BaseUrl + "/Account/Login";
    private const string Test1Email = "test1@test.com";
    private const string Test1Password = "Test!123";

    [Fact]
    public async Task ContactPage_DisplaysContactForm()
    {
        try
        {
            // Navigate to home page
            await Page.GotoAsync(BaseUrl);
            await Task.Delay(1000);

            // Click on Contact link
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Contact" }).ClickAsync();
            await Task.Delay(1000);

            // Verify all contact form fields are visible
            await Expect(Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Enter your name" }))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

            await Expect(Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Enter your email" }))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

            await Expect(Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Enter the subject" }))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

            await Expect(Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Enter your message" }))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task ContactPage_CanFillContactForm()
    {
        try
        {
            // Navigate to home page
            await Page.GotoAsync(BaseUrl);
            await Task.Delay(1000);

            // Click on Contact link
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Contact" }).ClickAsync();
            await Task.Delay(1000);

            // Fill in the contact form
            ILocator nameInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Enter your name" });
            await nameInput.FillAsync("Test User");

            ILocator emailInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Enter your email" });
            await emailInput.FillAsync("testuser@example.com");

            ILocator subjectInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Enter the subject" });
            await subjectInput.FillAsync("Test Subject");

            ILocator messageInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Enter your message" });
            await messageInput.FillAsync("This is a test message");

            // Verify all fields have been filled
            string nameValue = await nameInput.InputValueAsync();
            string emailValue = await emailInput.InputValueAsync();
            string subjectValue = await subjectInput.InputValueAsync();
            string messageValue = await messageInput.InputValueAsync();

            Assert.Equal("Test User", nameValue);
            Assert.Equal("testuser@example.com", emailValue);
            Assert.Equal("Test Subject", subjectValue);
            Assert.Equal("This is a test message", messageValue);
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task ContactPage_AccessibleFromLoggedInUser()
    {
        try
        {
            // Login as test1
            await LoginWithCredentials(Test1Email, Test1Password);

            // Click on Contact link
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Contact" }).ClickAsync();
            await Task.Delay(1000);

            // Verify contact form is displayed
            await Expect(Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Enter your name" }))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

            await Expect(Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Enter your email" }))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    #region Helper Methods

    /// <summary>
    /// Helper method to log in with specific credentials.
    /// </summary>
    private async Task LoginWithCredentials(string email, string password)
    {
        // Navigate to login page
        await Page.GotoAsync(LoginUrl);

        // Fill login form
        await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Your email" })
            .FillAsync(email);
        await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Password" })
            .FillAsync(password);

        // Click sign in button
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Sign in to your account" })
            .ClickAsync();

        // Wait for user menu to be visible (confirms login success)
        ILocator userMenuButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open user menu user photo" });
        await Expect(userMenuButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
        {
            Timeout = 30000
        });

        // Give the page a moment to fully settle after login
        await Task.Delay(1000);
    }

    #endregion
}
