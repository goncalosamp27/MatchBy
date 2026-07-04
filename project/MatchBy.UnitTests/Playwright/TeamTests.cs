using System.Globalization;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

public class TeamTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string LoginUrl = BaseUrl + "/Account/Login";
    private const string Test1Email = "test1@test.com";
    private const string Test1Password = "Test!123";
    private const string Test2Email = "test2@test.com";
    private const string Test2Password = "Test!123";

    [Fact]
    public async Task CreateTeam_SuccessfullyCreatesTeam()
    {
        string teamName = $"Test Team {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}";
        string teamDescription = "A test team for automated testing";

        try
        {
            // Login as test1
            await LoginWithCredentials(Test1Email, Test1Password);

            // Create a team
            await CreateTeam(teamName, teamDescription);

            // Verify team was created
            await Expect(Page.GetByText(teamName).First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });
        }
        finally
        {
            // Cleanup: Delete the team
            try
            {
                await DeleteTeam(teamName);
            }
            catch
            {
                // Ignore cleanup errors
            }
            await Page.CloseAsync();
        }
    }

   
    [Fact]
    public async Task InvitePlayerToTeam_PlayerReceivesNotification()
    {
        string teamName = $"Invite Team {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}";
        string teamDescription = "Team for invite test";

        try
        {
            // Login as test1
            await LoginWithCredentials(Test1Email, Test1Password);

            // Create a team with test2 invited
            await CreateTeamWithInvite(teamName, teamDescription, Test2Email);

            // Logout test1
            await Logout();
            await Task.Delay(1000);

            // Login as test2
            await LoginWithCredentials(Test2Email, Test2Password);

            // Click on notifications button
            ILocator notificationButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "View notifications" });
            await Expect(notificationButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
            await notificationButton.ClickAsync();
            await Task.Delay(1000);

            // Verify there's a team invite notification (check for team name in notifications)
            ILocator inviteNotification = Page.GetByText(teamName).First;
            await Expect(inviteNotification).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });
        }
        finally
        {
            // Cleanup: Delete the team
            try
            {
                await Logout();
                await Task.Delay(1000);
                await LoginWithCredentials(Test1Email, Test1Password);
                await DeleteTeam(teamName);
            }
            catch
            {
                // Ignore cleanup errors
            }
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

    /// <summary>
    /// Helper method to create a team.
    /// </summary>
    private async Task CreateTeam(string name, string description)
    {
        // Navigate to Teams page
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Teams" }).First.ClickAsync();
        await Task.Delay(1000);

        // Click "+ Create Team" button (try different variations)
        ILocator createTeamButton = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "+ Create Team" });
        bool createButtonExists = await createTeamButton.IsVisibleAsync();

        if (!createButtonExists)
        {
            createTeamButton = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Create Team" });
        }

        if (!await createTeamButton.IsVisibleAsync())
        {
            createTeamButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create Team" });
        }

        await Expect(createTeamButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await createTeamButton.ClickAsync();
        await Task.Delay(2000);

        // Fill team name
        ILocator nameInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Team name" });
        if (!await nameInput.IsVisibleAsync())
        {
            nameInput = Page.GetByRole(AriaRole.Textbox).First;
        }
        await Expect(nameInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await nameInput.FillAsync(name);

        // Fill team description
        ILocator descriptionInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Description" });
        if (!await descriptionInput.IsVisibleAsync())
        {
            descriptionInput = Page.GetByRole(AriaRole.Textbox).Nth(1);
        }
        await Expect(descriptionInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await descriptionInput.FillAsync(description);

        // Click Create/Submit button
        ILocator submitButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create Team" });
        if (!await submitButton.IsVisibleAsync())
        {
            submitButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create" });
        }
        if (!await submitButton.IsVisibleAsync())
        {
            submitButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Submit" });
        }

        await Expect(submitButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await submitButton.ClickAsync();

        // Wait for redirect/update
        await Task.Delay(3000);
    }

    /// <summary>
    /// Helper method to delete a team.
    /// </summary>
    private async Task DeleteTeam(string teamName)
    {
        // Navigate to Teams page
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Teams" }).First.ClickAsync();
        await Task.Delay(1000);

        // Find the team link (contains team name, "Owner", and email)
        ILocator teamLink = Page.GetByRole(AriaRole.Link).Filter(new LocatorFilterOptions { HasTextString = teamName }).First;
        await Expect(teamLink).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

        // Click on the team to view details
        await teamLink.ClickAsync();
        await Task.Delay(2000);

        // Click Delete Team button
        ILocator deleteButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Delete Team" });
        await Expect(deleteButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await deleteButton.ClickAsync();
        await Task.Delay(1000);
    }

    /// <summary>
    /// Helper method to create a team with an invited member.
    /// </summary>
    private async Task CreateTeamWithInvite(string name, string description, string inviteeEmail)
    {
        // Navigate to Teams page
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Teams" }).First.ClickAsync();
        await Task.Delay(1000);

        // Click "+ Create Team" button
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "+ Create Team" }).ClickAsync();
        await Task.Delay(2000);

        // Fill team name
        ILocator nameInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Enter team name" });
        await Expect(nameInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await nameInput.FillAsync(name);

        // Fill team description
        ILocator descriptionInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Share a brief description" });
        await Expect(descriptionInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await descriptionInput.FillAsync(description);

        // Select invitee from list
        await Page.GetByText(inviteeEmail, new PageGetByTextOptions { Exact = true }).ClickAsync();
        await Task.Delay(500);

        // Click Create team button
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create team" }).ClickAsync();
        await Task.Delay(3000);
    }

    /// <summary>
    /// Helper method to logout the current user.
    /// </summary>
    private async Task Logout()
    {
        // Click user menu
        ILocator userMenuButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open user menu user photo" });
        await Expect(userMenuButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await userMenuButton.ClickAsync();
        await Task.Delay(500);

        // Click logout button
        ILocator logoutButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Logout" });
        await Expect(logoutButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await logoutButton.ClickAsync();
        await Task.Delay(2000);
    }

    #endregion
}
