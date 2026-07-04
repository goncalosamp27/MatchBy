using System.Globalization;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

public class EditTeamTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string LoginUrl = BaseUrl + "/Account/Login";
    private const string Test1Email = "test1@test.com";
    private const string Test1Password = "Test!123";
    private const string Test2Email = "test2@test.com";
    private const string Test2Password = "Test!123";
    private static readonly string[] PrivateOption = new[] { "Private" };

    [Fact]
    public async Task EditTeam_UpdatesTeamInformation()
    {
        string teamName = $"Test Team {DateTime.Now.ToString("HHmmss", CultureInfo.InvariantCulture)}";
        string originalDescription = "Original description";
        string editedDescription = "Updated team description with new mission and goals";

        try
        {
            // Login as test1
            await LoginWithCredentials(Test1Email, Test1Password);

            // Navigate to Teams page
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Teams" }).ClickAsync();
            await Task.Delay(1000);

            // Create a new team
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "+ Create Team" }).ClickAsync();
            await Task.Delay(1000);

            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Enter team name" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Enter team name" }).FillAsync(teamName);

            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Share a brief description" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Share a brief description" }).FillAsync(originalDescription);

            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create team" }).ClickAsync();
            await Task.Delay(2000);

            // Click on the newly created team
            ILocator teamLink = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = teamName }).First;
            await Expect(teamLink).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
            await teamLink.ClickAsync();
            await Task.Delay(1000);

            // Click Edit Team button
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Edit Team" }).ClickAsync();
            await Task.Delay(1000);

            // Update the description
            ILocator descriptionInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Share your mission, cadence," });
            await Expect(descriptionInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
            await descriptionInput.ClickAsync();
            await descriptionInput.FillAsync(editedDescription);

            // Save changes
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Save changes" }).ClickAsync();
            await Task.Delay(2000);

            // Verify the updated description is displayed (should redirect back to team page)
            await Expect(Page.GetByText(editedDescription).First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });

            // Clean up: Delete the team (Delete Team button is on the team page)
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Delete Team" }).ClickAsync();
            await Task.Delay(2000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed with exception: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    [Fact]
    public async Task TeamPrivacy_PublicTeamVisibleToOthers_PrivateTeamNotVisible()
    {
        string teamName = $"Privacy Test Team {DateTime.Now.ToString("HHmmss", CultureInfo.InvariantCulture)}";
        string teamDescription = "Team for testing privacy settings";

        try
        {
            // === Part 1: Create public team as test1 ===
            await LoginWithCredentials(Test1Email, Test1Password);

            // Navigate to Teams page
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Teams" }).ClickAsync();
            await Task.Delay(1000);

            // Create a new public team
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "+ Create Team" }).ClickAsync();
            await Task.Delay(1000);

            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Enter team name" }).FillAsync(teamName);
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Share a brief description" }).FillAsync(teamDescription);
            
            // Ensure team is public (default should be public)
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create team" }).ClickAsync();
            await Task.Delay(2000);

            // === Part 2: Verify test2 can see the public team ===
            // Logout test1
            await LogoutUser();

            // Login as test2
            await LoginWithCredentials(Test2Email, Test2Password);

            // Navigate to Teams page
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Teams" }).ClickAsync();
            await Task.Delay(1000);

            // Click on "Search Teams" tab to see public teams
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Search Teams" }).ClickAsync();
            await Task.Delay(1000);

            // Verify test2 can see the public team
            ILocator publicTeamLink = Page.GetByText(teamName).First;
            await Expect(publicTeamLink).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

            // === Part 3: Change team to private as test1 ===
            // Logout test2
            await LogoutUser();

            // Login back as test1
            await LoginWithCredentials(Test1Email, Test1Password);

            // Navigate to Teams page and open the team
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Teams" }).ClickAsync();
            await Task.Delay(1000);

            ILocator teamLink = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = teamName }).First;
            await teamLink.ClickAsync();
            await Task.Delay(1000);

            // Click Edit Team button
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Edit Team" }).ClickAsync();
            await Task.Delay(1000);

            // Change privacy to Private
            await Page.GetByRole(AriaRole.Combobox).SelectOptionAsync(PrivateOption);

            // Save changes
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Save changes" }).ClickAsync();
            await Task.Delay(2000);

            // === Part 4: Verify test2 cannot see the private team ===
            // Logout test1
            await LogoutUser();

            // Login as test2
            await LoginWithCredentials(Test2Email, Test2Password);

            // Navigate to Teams page
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Teams" }).ClickAsync();
            await Task.Delay(1000);

            // Click on "Search Teams" tab to look for teams
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Search Teams" }).ClickAsync();
            await Task.Delay(1000);

            // Verify test2 cannot see the private team
            ILocator privateTeamLink = Page.GetByText(teamName).First;
            bool isPrivateTeamVisible = await privateTeamLink.IsVisibleAsync();
            Assert.False(isPrivateTeamVisible, "Private team should not be visible to non-members");

            // === Clean up: Delete the team ===
            // Logout test2
            await LogoutUser();

            // Login back as test1
            await LoginWithCredentials(Test1Email, Test1Password);

            // Navigate to Teams page and open the team
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Teams" }).ClickAsync();
            await Task.Delay(1000);

            ILocator teamLinkToDelete = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = teamName }).First;
            await teamLinkToDelete.ClickAsync();
            await Task.Delay(1000);

            // Delete the team
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Delete Team" }).ClickAsync();
            await Task.Delay(2000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed with exception: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private async Task LogoutUser()
    {
        // Click user menu
        ILocator userMenuButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open user menu user photo" });
        await userMenuButton.ClickAsync();
        await Task.Delay(500);

        // Click logout button (it's a button inside a form, not a link)
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Logout" }).ClickAsync();
        await Task.Delay(2000);
    }

    private async Task LoginWithCredentials(string email, string password)
    {
        await Page.GotoAsync(LoginUrl);
        
        // Fill in email
        await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Email" })
            .FillAsync(email);
        
        // Fill in password
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
}
