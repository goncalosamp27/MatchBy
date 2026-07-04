using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

public class LoggedInHomePageTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string HomeUrl = BaseUrl + "/";
    private const string LoginUrl = BaseUrl + "/Account/Login";
    private const string ValidEmail = "test1@test.com";
    private const string ValidPassword = "Test!123";

    #region Authenticated User Tests

    [Fact]
    public async Task LoggedInHomePage_ShowsStartPlayingButton()
    {
        try
        {
            // Arrange - Login first
            await LoginAsAdmin();

            // Act - Navigate to home page
            await Page.GotoAsync(HomeUrl);

            // Assert - "Start Playing!" button visible for authenticated users
            ILocator startPlayingButton = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Start Playing!" });
            await Expect(startPlayingButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });

            // Assert - "Join now!" button should NOT be visible
            ILocator joinButton = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Join now!" });
            await Expect(joinButton).Not.ToBeVisibleAsync();
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task LoggedInHomePage_StartPlayingNavigatesToMatches()
    {
        try
        {
            // Arrange - Login first
            await LoginAsAdmin();
            await Page.GotoAsync(HomeUrl);

            // Act - Click "Start Playing!" button
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Start Playing!" }).ClickAsync();

            // Assert - Navigated to Matches page
            await Page.WaitForURLAsync("**/Matches", new PageWaitForURLOptions { Timeout = 10000 });
            await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Matches", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task LoggedInHomePage_ShowsPersonalizedWelcomeMessage()
    {
        try
        {
            // Arrange - Login first
            await LoginAsAdmin();

            // Act - Navigate to home page
            await Page.GotoAsync(HomeUrl);

            // Assert - The page shows random messages including personalized ones with username
            // Messages include: "Hey {username}, are you ready for your next match?" or "Welcome back, {username}! Ready for another game?"
            ILocator heroHeading = Page.Locator("h1.text-4xl, h1.text-5xl, h1.text-6xl").First;
            await Expect(heroHeading).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 30000
            });

            string? headingText = await heroHeading.TextContentAsync();
            Assert.NotNull(headingText);
            Assert.NotEmpty(headingText.Trim());
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task LoggedInHomePage_LetsGoButtonNavigatesToCreateMatch()
    {
        try
        {
            // Arrange - Login first
            await LoginAsAdmin();
            await Page.GotoAsync(HomeUrl);

            // Act - Click "Let's go!" button in "Create your Match!" section
            await Page.GetByText("Let's go!").ClickAsync();

            // Assert - Navigated to create match page
            await Page.WaitForURLAsync("**/matches/create", new PageWaitForURLOptions { Timeout = 10000 });
            await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/matches/create", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task LoggedInHomePage_LearnMoreButtonNavigatesToAbout()
    {
        try
        {
            // Arrange - Login first
            await LoginAsAdmin();
            await Page.GotoAsync(HomeUrl);

            // Act - Click "Learn more" button
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Learn more" }).ClickAsync();

            // Assert - Navigated to About page
            await Page.WaitForURLAsync("**/About", new PageWaitForURLOptions { Timeout = 10000 });
            await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/About", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task LoggedInHomePage_DisplaysCreateMatchSection()
    {
        try
        {
            // Arrange - Login first
            await LoginAsAdmin();

            // Act - Navigate to home page
            await Page.GotoAsync(HomeUrl);

            // Assert - "Create your Match!" heading visible
            await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Create your Match!" }))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

            // Assert - Description text visible
            await Expect(Page.GetByText("Easily create, organize, share, and manage matches."))
                .ToBeVisibleAsync();

            // Assert - "Let's go!" button visible
            await Expect(Page.GetByText("Let's go!")).ToBeVisibleAsync();
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task LoggedInHomePage_UserMenuDisplaysEmail()
    {
        try
        {
            // Arrange - Login first
            await LoginAsAdmin();
            await Page.GotoAsync(HomeUrl);

            // Act - Click user menu button
            ILocator userMenuButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open user menu user photo" });
            await Expect(userMenuButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
            await userMenuButton.ClickAsync();

            // Assert - Email displayed in dropdown menu
            await Expect(Page.GetByText(ValidEmail).First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task LoggedInHomePage_CarouselVisible_OnDesktop()
    {
        try
        {
            // Arrange - Login and set viewport to desktop size
            await LoginAsAdmin();
            await Page.SetViewportSizeAsync(1920, 1080);

            // Act - Navigate to home page
            await Page.GotoAsync(HomeUrl);

            // Assert - At least one sport image should be visible in carousel
            ILocator carouselImages = Page.Locator("img[src*='carousel/']");
            await Expect(carouselImages.First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 30000
            });

            // Verify at least one sport image is present
            int imageCount = await carouselImages.CountAsync();
            Assert.True(imageCount > 0, "Expected at least one sport image in carousel");
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task LoggedInHomePage_LogoutButtonWorksCorrectly()
    {
        try
        {
            // Arrange - Login first
            await LoginAsAdmin();
            await Page.GotoAsync(HomeUrl);

            // Act - Open user menu
            ILocator userMenuButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open user menu user photo" });
            await Expect(userMenuButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
            await userMenuButton.ClickAsync();

            // Wait for dropdown to be visible
            await Task.Delay(500);

            // Click logout button
            ILocator logoutButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Logout" });
            await Expect(logoutButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
            await logoutButton.ClickAsync();

            // Assert - Redirected to home page and user is logged out
            await Task.Delay(1000); // Wait for logout to complete
            
            // After logout, user should see "Join now!" instead of user menu
            ILocator joinButton = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Join now!" });
            await Expect(joinButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });

            // User menu should not be visible after logout
            ILocator userMenuAfterLogout = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open user menu user photo" });
            await Expect(userMenuAfterLogout).Not.ToBeVisibleAsync();
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

  

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method to log in as admin user before tests.
    /// Waits for successful login and home page redirect.
    /// </summary>
    private async Task LoginAsAdmin()
    {
        // Navigate to login page
        await Page.GotoAsync(LoginUrl);

        // Fill login form
        await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Your email" })
            .FillAsync(ValidEmail);
        await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Password" })
            .FillAsync(ValidPassword);

        // Click sign in button
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Sign in to your account" })
            .ClickAsync();

        // Wait for navigation after successful login (redirects to home page)
        await Page.WaitForURLAsync("**/", new PageWaitForURLOptions { Timeout = 15000 });

        // Wait for user menu to be visible (confirms login success)
        ILocator userMenuButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open user menu user photo" });
        await Expect(userMenuButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
        {
            Timeout = 30000
        });
    }

    #endregion
}
