using System.Globalization;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

public class MatchTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string LoginUrl = BaseUrl + "/Account/Login";

    // Test user credentials
    private const string Test1Email = "test1@test.com";
    private const string Test1Password = "Test!123";
    private const string Test2Email = "test2@test.com";
    private const string Test2Password = "Test!123";

    [Fact]
    public async Task CreateMatchTest()
    {
        string matchAddress = $"Test Match {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}";
        string matchDescription = "Test match created by test1";
        try
        {
            // Generate unique match details

            // Test1 logs in and creates a match
            await LoginWithCredentials(Test1Email, Test1Password);
            await CreateMatch(matchAddress, matchDescription);

            // Verify match was created
            await Expect(Page.GetByText(matchAddress)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });
        }
        finally
        {
            // Cleanup: Delete the match
            try
            {
                await Logout();
                await Task.Delay(1000);
                await LoginWithCredentials(Test1Email, Test1Password);
                await DeleteMatch(matchAddress);
            }
            catch
            {
                // Ignore cleanup errors
            }
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task CreateAndDeleteMatch()
    {
        string matchAddress = $"Test Match {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}";

        try
        {
            // Generate unique match details
            string matchDescription = "Test match to be deleted";

            // Test1 logs in and creates a match
            await LoginWithCredentials(Test1Email, Test1Password);
            await CreateMatch(matchAddress, matchDescription);

            // Verify match was created
            await Expect(Page.GetByText(matchAddress)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });

            // Test1 deletes the match
            await DeleteMatch(matchAddress);

            // Verify match was deleted - navigate to matches page and check it's not visible
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Matches" }).ClickAsync();
            await Task.Delay(2000);

            bool matchStillExists = await Page.GetByText(matchAddress).IsVisibleAsync();
            Assert.False(matchStillExists, "Match should be deleted and not visible");
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task JoinMatch_Test1CreatesTest2Joins_Test2JoinsSuccessfully()
    {
        string matchAddress = $"Test Match {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}";

        try
        {
            // Generate unique match details
            string matchDescription = "Test match for joining";

            // Test1 creates a match
            await LoginWithCredentials(Test1Email, Test1Password);
            await CreateMatch(matchAddress, matchDescription);

            // Verify match was created
            await Expect(Page.GetByText(matchAddress)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });

            // Logout test1
            await Logout();
            await Task.Delay(1000);

            // Test2 joins the match
            await LoginWithCredentials(Test2Email, Test2Password);
            await JoinMatch(matchAddress);

            // Verify test2 joined successfully (check in "Matches I'm Attending" section)
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Matches" }).ClickAsync();
            await Task.Delay(1000);
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "My Matches" }).ClickAsync();
            await Task.Delay(1000);

            await Expect(Page.GetByText(matchAddress)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });
        }
        finally
        {
            // Cleanup: Delete the match
            try
            {
                await Logout();
                await Task.Delay(1000);
                await LoginWithCredentials(Test1Email, Test1Password);
                await DeleteMatch(matchAddress);
            }
            catch
            {
                // Ignore cleanup errors
            }

            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task JoinAndLeaveMatch_Test1CreatesTest2JoinsAndLeaves_Test2LeavesSuccessfully()
    {
        string matchAddress = $"Test Match {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}";
        
        try
        {
            // Generate unique match details
            string matchDescription = "Test match for join and leave";
    
            // Test1 creates a match
            await LoginWithCredentials(Test1Email, Test1Password);
            await CreateMatch(matchAddress, matchDescription);
            
            // Verify match was created
            await Expect(Page.GetByText(matchAddress)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });
            
            // Logout test1
            await Logout();
            await Task.Delay(1000);
    
            // Test2 joins the match
            await LoginWithCredentials(Test2Email, Test2Password);
            await JoinMatch(matchAddress);
            
            // Verify test2 joined successfully
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Matches" }).ClickAsync();
            await Task.Delay(1000);
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "My Matches" }).ClickAsync();
            await Task.Delay(1000);
            
            await Expect(Page.GetByText(matchAddress)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });
    
            // Test2 leaves the match
            await LeaveMatch(matchAddress);
            
            // Verify we're back on Matches page
            await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Matches", Exact = true }))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
                {
                    Timeout = 10000
                });
        }
        finally
        {
            // Cleanup: Delete the match
            try
            {
                await Logout();
                await Task.Delay(1000);
                await LoginWithCredentials(Test1Email, Test1Password);
                await DeleteMatch(matchAddress);
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
    /// Helper method to create a match with given details.
    /// </summary>
    private async Task CreateMatch(string address, string description)
    {
        // Navigate to Matches page
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Matches" }).ClickAsync();
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

        // Click Create Match button
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create Match" }).ClickAsync();

        // Wait for redirect
        await Task.Delay(3000);
    }

    /// <summary>
    /// Helper method to join a match by searching for it and clicking join.
    /// </summary>
    private async Task JoinMatch(string matchAddress)
    {
        // Navigate to Matches page
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Matches" }).ClickAsync();
        await Task.Delay(1000);

        // Switch to Search Matches tab
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Search Matches" }).ClickAsync();
        await Task.Delay(1000);

        // Find the match by address
        ILocator matchCard = Page.GetByText(matchAddress).First;
        await Expect(matchCard).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

        // Click on the match to view details
        await matchCard.ClickAsync();
        await Task.Delay(2000);

        // Click Join Match button (try both "Join Match" and "Join Match!")
        ILocator joinButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Join Match!" });
        bool joinButtonExists = await joinButton.IsVisibleAsync();

        if (!joinButtonExists)
        {
            joinButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Join Match" });
        }

        await Expect(joinButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await joinButton.ClickAsync();
        await Task.Delay(2000);
    }

    /// <summary>
    /// Helper method to leave a match by finding it and clicking leave.
    /// </summary>
    private async Task LeaveMatch(string matchAddress)
    {
        // Navigate to Matches page
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Matches" }).ClickAsync();
        await Task.Delay(1000);

        // Switch to Search Matches tab to find the match
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Search Matches" }).ClickAsync();
        await Task.Delay(1000);

        // Find the match by address
        ILocator matchCard = Page.GetByText(matchAddress).First;
        await Expect(matchCard).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

        // Click on the match to view details
        await matchCard.ClickAsync();
        await Task.Delay(2000);

        // Click Leave Match button
        ILocator leaveButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Leave Match" });
        await Expect(leaveButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await leaveButton.ClickAsync();
        await Task.Delay(2000);
    }

    /// <summary>
    /// Helper method to delete a match by finding it in "My Matches" and clicking delete.
    /// </summary>
    private async Task DeleteMatch(string matchAddress)
    {
        // Navigate to Matches page
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Matches" }).ClickAsync();
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
