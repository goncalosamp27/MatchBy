using System.Globalization;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

public class MatchSkillLevelTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string LoginUrl = BaseUrl + "/Account/Login";
    private const string ValidEmail = "test1@test.com";
    private const string ValidPassword = "Test!123";

    [Fact]
    public async Task CreateMatch_WithThreeStarsSkillLevel_ShowsSkillLevelOnMatchDetails()
    {
        string matchAddress = $"Skill Test Match {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}";
        string matchDescription = "Testing three stars skill level";
        string skillLevel = "ThreeStars";

        try
        {
            await LoginWithCredentials(ValidEmail, ValidPassword);
            await CreateMatchWithSkillLevel(matchAddress, matchDescription, skillLevel);

            // Verify skill level is displayed on match details page
            await Expect(Page.GetByText("Three Stars").First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });

            // Delete the match
            await DeleteMatch(matchAddress);
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    #region Helper Methods

    /// <summary>
    /// Helper method to log in with specific credentials (from MatchTests.cs).
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

    /// <summary>
    /// Helper method to create a match with skill level (adapted from MatchTests.cs).
    /// </summary>
    private async Task CreateMatchWithSkillLevel(string address, string description, string skillLevel)
    {
        // Navigate to Matches page
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Matches" }).First.ClickAsync();
        await Task.Delay(1000);

        // Click "+ Create Match" button (try both possible button texts)
        ILocator createMatchButton = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "+ Create Match" });
        bool createFirstMatchExists = await createMatchButton.IsVisibleAsync();

        if (!createFirstMatchExists)
        {
            // If "+ Create Match" is not visible, try "Create Match" (header button)
            createMatchButton = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Create Match" });
        }

        await Expect(createMatchButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await createMatchButton.ClickAsync();
        await Task.Delay(2000);

        // Fill match address
        ILocator addressInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Type your match address" });
        await Expect(addressInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await addressInput.ClickAsync();
        await addressInput.FillAsync(address);

        // Fill match description
        ILocator descriptionInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Type a description for your" });
        await Expect(descriptionInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await descriptionInput.ClickAsync();
        await descriptionInput.FillAsync(description);

        // Select skill level (third combobox in the form)
        ILocator skillCombobox = Page.GetByRole(AriaRole.Combobox).Nth(2);
        await Expect(skillCombobox).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await skillCombobox.SelectOptionAsync(new[] { skillLevel });

        // Click Create Match button
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create Match" }).ClickAsync();

        // Wait for redirect
        await Task.Delay(3000);
    }

    /// <summary>
    /// Helper method to delete a match (from MatchTests.cs).
    /// </summary>
    private async Task DeleteMatch(string matchAddress)
    {
        // Navigate to Matches page
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Matches" }).First.ClickAsync();
        await Task.Delay(1000);

        // Switch to My Matches tab
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "My Matches" }).ClickAsync();
        await Task.Delay(1000);

        // Find the match by address
        ILocator matchCard = Page.GetByText(matchAddress).First;
        await Expect(matchCard).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

        // Click on the match to view details
        await matchCard.ClickAsync();
        await Task.Delay(2000);

        // Click Delete Match button
        ILocator deleteButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Cancel Match" });
        await Expect(deleteButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await deleteButton.ClickAsync();
        await Task.Delay(1000);
    }

    #endregion
}
