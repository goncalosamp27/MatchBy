using System.Globalization;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

public class EditMessageTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string LoginUrl = BaseUrl + "/Account/Login";
    private const string Test1Email = "test1@test.com";
    private const string Test1Password = "Test!123";

    [Fact]
    public async Task EditMessage_UpdatesMessageContent()
    {
        string originalMessage = $"Original message {DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}";
        string editedMessage = $"Edited message {DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}";

        try
        {
            // Login as test1
            await LoginWithCredentials(Test1Email, Test1Password);

            // Navigate to Chat
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

            // Send a message
            ILocator messageInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Write a message..." });
            await Expect(messageInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
            await messageInput.FillAsync(originalMessage);
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Send" }).ClickAsync();
            await Task.Delay(1000);

            // Verify original message appears
            await Expect(Page.GetByText(originalMessage).First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });

            // Wait a bit for the message to fully render
            await Task.Delay(1000);

            // Click the dropdown menu button (three dots) on the message
            // Find the button next to the message
            ILocator dropdownButton = Page.Locator("button").Filter(new LocatorFilterOptions
            {
                HasText = ""
            }).Last; // Get the last one which should be for our message
            await dropdownButton.ClickAsync();
            await Task.Delay(500);

            // Click Edit button from the dropdown menu
            ILocator editButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Edit" });
            await editButton.ClickAsync();
            await Task.Delay(500);

            // Edit the message
            ILocator editInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Edit message..." });
            await Expect(editInput).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
            await editInput.FillAsync(editedMessage);

            // Click Save or Send button
            ILocator saveButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Save" });
            bool saveButtonExists = await saveButton.IsVisibleAsync();

            if (saveButtonExists)
            {
                await saveButton.ClickAsync();
            }
            else
            {
                ILocator sendButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Send" });
                await sendButton.ClickAsync();
            }
            await Task.Delay(1000);

            // Verify edited message appears
            await Expect(Page.GetByText(editedMessage).First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });

            // Verify the message shows "edited" indicator
            ILocator editedIndicator = Page.Locator("text=/edited/i").First;
            bool hasEditedIndicator = await editedIndicator.IsVisibleAsync();
            Assert.True(hasEditedIndicator, "Message should show 'edited' indicator after being edited");

            // Now delete the message
            await Task.Delay(1000);
            
            // Click the dropdown menu button again
            ILocator dropdownButton2 = Page.Locator("button").Filter(new LocatorFilterOptions
            {
                HasText = ""
            }).Last;
            await dropdownButton2.ClickAsync();
            await Task.Delay(500);

            // Click Delete button
            ILocator deleteButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Delete" });
            await deleteButton.ClickAsync();
            await Task.Delay(1000);

            // Verify the message is deleted (no longer visible)
            bool messageStillVisible = await Page.GetByText(editedMessage).First.IsVisibleAsync();
            Assert.False(messageStillVisible, "Message should be deleted and no longer visible");
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task EditMessage_ShowsEditedIndicator()
    {
        string originalMessage = $"Test msg {DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}";
        string editedMessage = $"Updated msg {DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}";

        try
        {
            // Login as test1
            await LoginWithCredentials(Test1Email, Test1Password);

            // Navigate to Chat
            ILocator userMenuButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open user menu user photo" });
            await userMenuButton.ClickAsync();
            await Task.Delay(500);

            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Chat" }).ClickAsync();
            await Task.Delay(1000);

            // Click on a match chat
            ILocator matchChatButton = Page.GetByRole(AriaRole.Button).Filter(new LocatorFilterOptions
            {
                HasTextString = "Match"
            }).First;
            await matchChatButton.ClickAsync();
            await Task.Delay(1000);

            // Send a message
            ILocator messageInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Write a message..." });
            await messageInput.FillAsync(originalMessage);
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Send" }).ClickAsync();
            await Task.Delay(1000);

            // Verify message appears
            await Expect(Page.GetByText(originalMessage).First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });
            await Task.Delay(1000);

            // Click the dropdown menu button (three dots) on the message
            ILocator dropdownButton = Page.Locator("button").Filter(new LocatorFilterOptions
            {
                HasText = ""
            }).Last;
            await dropdownButton.ClickAsync();
            await Task.Delay(500);

            // Click Edit
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Edit" }).ClickAsync();
            await Task.Delay(500);

            // Edit the message
            ILocator editInput = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Edit message..." });
            await editInput.FillAsync(editedMessage);

            // Save the edit
            ILocator saveButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Save" });
            if (await saveButton.IsVisibleAsync())
            {
                await saveButton.ClickAsync();
            }
            else
            {
                await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Send" }).ClickAsync();
            }
            await Task.Delay(1000);

            // Verify the edited message is visible (don't refresh page)
            bool editedMessageVisible = await Page.GetByText(editedMessage).First.IsVisibleAsync();
            Assert.True(editedMessageVisible, "Edited message should be visible without refreshing");

            // Verify original message is no longer visible
            bool originalMessageStillVisible = await Page.GetByText(originalMessage).First.IsVisibleAsync();
            Assert.False(originalMessageStillVisible, "Original message should be replaced by edited version");

            // Now delete the message
            await Task.Delay(1000);
            
            // Click the dropdown menu button again
            ILocator dropdownButton2 = Page.Locator("button").Filter(new LocatorFilterOptions
            {
                HasText = ""
            }).Last;
            await dropdownButton2.ClickAsync();
            await Task.Delay(500);

            // Click Delete button
            ILocator deleteButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Delete" });
            await deleteButton.ClickAsync();
            await Task.Delay(1000);

            // Verify the message is deleted (no longer visible)
            bool messageDeleted = await Page.GetByText(editedMessage).First.IsVisibleAsync();
            Assert.False(messageDeleted, "Message should be deleted and no longer visible");
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
