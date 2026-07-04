using System.Globalization;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

public class ChatTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string LoginUrl = BaseUrl + "/Account/Login";
    private const string Test1Email = "test1@test.com";
    private const string Test1Password = "Test!123";
    // private const string Test2Email = "test2@test.com";
    // private const string Test2Password = "Test!123";

    [Fact]
    public async Task MatchChat_SendMessage_MessageAppearsInChat()
    {
        string testMessage = $"Test message {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}";

        try
        {
            // Login as test1
            await LoginWithCredentials(Test1Email, Test1Password);

            // Navigate to Chat (open user menu first)
            ILocator userMenuButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open user menu user photo" });
            await userMenuButton.ClickAsync();
            await Task.Delay(500);
            
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Chat" }).ClickAsync();
            await Task.Delay(1000);

            // Click on the first available match chat
            ILocator matchChatButton = Page.GetByRole(AriaRole.Button).Filter(new LocatorFilterOptions 
            { 
                HasTextString = "Match" 
            }).First;
            await Expect(matchChatButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
            await matchChatButton.ClickAsync();
            await Task.Delay(1000);

            // Type and send a message
            ILocator messageInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Write a message..." });
            await Expect(messageInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
            await messageInput.ClickAsync();
            await messageInput.FillAsync(testMessage);

            // Click Send button
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Send" }).ClickAsync();
            await Task.Delay(1000);

            // Verify the message appears in the chat
            await Expect(Page.GetByText(testMessage).First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    // [Fact]
    // public async Task MatchChat_RealTimeMessaging_Test1SendsTest2Receives()
    // {
    //     string testMessage = $"Real-time test {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}";
    //     string matchName = $"Chat Test Match {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}";

    //     try
    //     {
    //         // Test1 creates a match
    //         await LoginWithCredentials(Test1Email, Test1Password);
    //         await CreateMatch(matchName, "Test match for chat");

    //         // Test1 logs out
    //         await Logout();
    //         await Task.Delay(1000);

    //         // Test2 joins the match
    //         await LoginWithCredentials(Test2Email, Test2Password);
    //         await JoinMatch(matchName);

    //         // Test2 logs out
    //         await Logout();
    //         await Task.Delay(1000);

    //         // Test1 logs back in and sends a chat message
    //         await LoginWithCredentials(Test1Email, Test1Password);
            
    //         // Open user menu and navigate to Chat
    //         ILocator userMenuButton1 = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open user menu user photo" });
    //         await userMenuButton1.ClickAsync();
    //         await Task.Delay(500);
            
    //         await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Chat" }).ClickAsync();
    //         await Task.Delay(1000);

    //         // Find and click the match chat by name (use partial match)
    //         ILocator matchChat = Page.GetByRole(AriaRole.Button).Filter(new LocatorFilterOptions { HasTextRegex = new System.Text.RegularExpressions.Regex("Chat Test Match") }).First;
    //         await Expect(matchChat).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
    //         await matchChat.ClickAsync();
    //         await Task.Delay(1000);

    //         // Send message
    //         ILocator messageInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Write a message..." });
    //         await messageInput.FillAsync(testMessage);
    //         await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Send" }).ClickAsync();
    //         await Task.Delay(1000);

    //         // Verify message sent
    //         await Expect(Page.GetByText(testMessage).First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
    //         {
    //             Timeout = 10000
    //         });

    //         // Logout test1
    //         await Logout();
    //         await Task.Delay(1000);

    //         // Test2 logs in and verifies they can see test1's message
    //         await LoginWithCredentials(Test2Email, Test2Password);
            
    //         // Open user menu and navigate to Chat
    //         ILocator userMenuButton2 = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open user menu user photo" });
    //         await userMenuButton2.ClickAsync();
    //         await Task.Delay(500);
            
    //         await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Chat" }).ClickAsync();
    //         await Task.Delay(1000);

    //         // Open the same match chat (use partial match)
    //         ILocator matchChat2 = Page.GetByRole(AriaRole.Button).Filter(new LocatorFilterOptions { HasTextRegex = new System.Text.RegularExpressions.Regex("Chat Test Match") }).First;
    //         await Expect(matchChat2).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
    //         await matchChat2.ClickAsync();
    //         await Task.Delay(1000);

    //         // Verify test2 can see test1's message
    //         await Expect(Page.GetByText(testMessage).First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
    //         {
    //             Timeout = 10000
    //         });
    //     }
    //     finally
    //     {
    //         // Cleanup: Delete the match
    //         try
    //         {
    //             await Logout();
    //             await Task.Delay(1000);
    //             await LoginWithCredentials(Test1Email, Test1Password);
    //             await DeleteMatch(matchName);
    //         }
    //         catch
    //         {
    //             // Ignore cleanup errors
    //         }
    //         await Page.CloseAsync();
    //     }
    // }

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
    /// Helper method to create a match.
    /// </summary>
    private async Task CreateMatch(string address, string description)
    {
        // Navigate to Matches page
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Matches" }).First.ClickAsync();
        await Task.Delay(1000);

        // Click "+ Create Match" button
        ILocator createMatchButton = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "+ Create Match" });
        bool createFirstMatchExists = await createMatchButton.IsVisibleAsync();

        if (!createFirstMatchExists)
        {
            createMatchButton = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Create Match" });
        }

        await Expect(createMatchButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await createMatchButton.ClickAsync();
        await Task.Delay(2000);

        // Fill match address
        ILocator addressInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Type your match address" });
        await Expect(addressInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await addressInput.FillAsync(address);

        // Fill match description
        ILocator descriptionInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Type a description for your" });
        await Expect(descriptionInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await descriptionInput.FillAsync(description);

        // Click Create Match button
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create Match" }).ClickAsync();

        // Wait for redirect
        await Task.Delay(3000);
    }

    /// <summary>
    /// Helper method to join a match.
    /// </summary>
    private async Task JoinMatch(string matchAddress)
    {
        // Navigate to Matches page
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Matches" }).First.ClickAsync();
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

        // Click Join Match button
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
    /// Helper method to delete a match.
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
