using System.Globalization;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

/// <summary>
/// End-to-end test for creating a new match.
/// User Story: As a user, I want to create a new match, so that other players can join.
/// 
/// Test covers the complete flow:
/// 1. User is logged in
/// 2. User fills in match details (sport, location, date, skill level)
/// 3. User clicks "Create Match"
/// 4. System creates the match
/// 5. Match appears in the matches list
/// </summary>
public class CreateMatchTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string LoginUrl = BaseUrl + "/Account/Login";
    private const string MatchesUrl = BaseUrl + "/Matches";
    private const string CreateMatchUrl = BaseUrl + "/matches/create";
    
    // Test user credentials - admin user
    private const string TestEmail = "admin@admin.com";
    private const string TestPassword = "Admin!123";

    [Fact]
    public async Task CreateMatch_UserFillsDetailsAndCreates_MatchAppearsInList()
    {
        try
        {
            // Arrange - Login as admin
            await LoginAsUser();
            
            // Generate unique match name to verify it appears
            string matchName = $"Test Match {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}";

            // Act - Navigate to Create Match page
            await Page.GotoAsync(CreateMatchUrl);
            await Task.Delay(2000); // Wait for page to load

            // Act - Fill in match details
            // Match name
            ILocator matchNameInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Match Name" });
            await Expect(matchNameInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
            await matchNameInput.FillAsync(matchName);

            // Sport (select from dropdown)
            ILocator sportSelect = Page.GetByLabel("Sport");
            await Expect(sportSelect).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
            await sportSelect.SelectOptionAsync("Football");
            
            // Location
            ILocator locationInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Location" });
            await Expect(locationInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
            await locationInput.FillAsync("Test Stadium");

            // Date (set to tomorrow)
            string tomorrowDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ILocator dateInput = Page.GetByLabel("Date");
            await Expect(dateInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
            await dateInput.FillAsync(tomorrowDate);

            // Time
            ILocator timeInput = Page.GetByLabel("Time");
            await Expect(timeInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
            await timeInput.FillAsync("18:00");

            // Skill Level (if exists) - use try/catch since IsVisibleAsync doesn't support custom timeout anymore
            try
            {
                ILocator skillLevelSelect = Page.GetByLabel("Skill Level");
                bool skillLevelExists = await skillLevelSelect.IsVisibleAsync();
                if (skillLevelExists)
                {
                    await skillLevelSelect.SelectOptionAsync("Intermediate");
                }
            }
            catch
            {
                // Skill level field doesn't exist, continue without it
            }

            // Act - Click Create Match button
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create Match" }).ClickAsync();
            
            // Wait for redirect to matches list or match details
            await Task.Delay(3000);

            // Assert - Navigate to Matches page to verify the match appears
            await Page.GotoAsync(MatchesUrl);
            await Task.Delay(2000);

            // Assert - Match with the unique name is visible in the list
            await Expect(Page.GetByText(matchName)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    #region Helper Methods

    /// <summary>
    /// Helper method to log in as admin user.
    /// </summary>
    private async Task LoginAsUser()
    {
        await LoginWithCredentials(TestEmail, TestPassword);
    }

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

