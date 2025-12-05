using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

/// <summary>
/// End-to-end tests for viewing other players' profiles.
/// User Story: As a user, I want to view other players' profiles, so that I can decide whether to join matches with them.
/// 
/// Tests cover the complete flow:
/// 1. User is logged in
/// 2. User navigates to Matches page
/// 3. User selects a match they are attending
/// 4. User views participants list
/// 5. User clicks on another player's profile
/// 6. System displays that player's profile information (bio, photo, ratings, preferences)
/// </summary>
public class ViewPlayerProfileTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string LoginUrl = BaseUrl + "/Account/Login";
    private const string MatchesUrl = BaseUrl + "/Matches";

    // Test user credentials - admin user
    private const string TestEmail = "admin@admin.com";
    private const string TestPassword = "Admin!123";

    #region View Player Profile Flow Tests

    [Fact]
    public async Task ViewPlayerProfile_UserNavigatesToMatches_MatchesPageLoads()
    {
        try
        {
            // Arrange - Login as admin
            await LoginAsAdmin();

            // Act - Navigate to Matches page
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Matches" }).ClickAsync();

            // Assert - Matches page loaded
            await Page.WaitForURLAsync("**/matches", new PageWaitForURLOptions { Timeout = 10000 });
            await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Matches", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
            
            // Assert - Breadcrumb with "Matches" text is visible (more specific than just "Matches")
            await Expect(Page.Locator("nav[aria-label='Breadcrumb']").GetByText("Matches")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
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
    public async Task ViewPlayerProfile_UserClicksOnAnotherPlayerProfile_ProfileDisplaysInNewTab()
    {
        try
        {
            // Arrange - Login, navigate to match, expand participants
            await LoginAsAdmin();
            await Page.GotoAsync(MatchesUrl);
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Football image Football Match" }).ClickAsync();
            await Task.Delay(2000);

            ILocator participantsSection = Page.Locator(".bg-\\[var\\(--section\\)\\].rounded-lg > .flex.items-center.justify-between").First;
            await participantsSection.ClickAsync();
            await Task.Delay(500);

            // Act - Click on another player's profile link (user1@user.com 5.0) - opens in new tab
            IPage profilePage = await Page.RunAndWaitForPopupAsync(async () =>
            {
                await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "user1@user.com 5.0" }).ClickAsync();
            });

            // Assert - Profile page opened in new tab
            await profilePage.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert - Profile URL contains "profile"
            string profileUrl = profilePage.Url;
            Assert.Contains("profile", profileUrl.ToLower());

            // Assert - Player's username/email is displayed as heading
            await Expect(profilePage.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "user1@user.com" }))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

            // Clean up
            await profilePage.CloseAsync();
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    #endregion

    #region Helper Methods

    private async Task LoginAsAdmin()
    {
        await LoginWithCredentials(TestEmail, TestPassword);
    }


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
