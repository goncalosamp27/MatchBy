using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

/// <summary>
/// End-to-end test for adding a bio to user profile.
/// User Story: As a user, I want to add a short bio, so that others can understand my interests.
/// 
/// Test covers the complete flow:
/// 1. User is logged in
/// 2. User navigates to Account Settings
/// 3. User edits their profile and adds a bio
/// 4. System saves the bio
/// 5. Bio is displayed on the user's profile page
/// </summary>
public class AddBioToProfileTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string LoginUrl = BaseUrl + "/Account/Login";
    
    // Test user credentials - admin user
    private const string TestEmail = "admin@admin.com";
    private const string TestPassword = "Admin!123";

    [Fact]
    public async Task AddBioToProfile_UserAddsAndSavesBio_BioIsSavedAndDisplayed()
    {
        try
        {
            // Arrange - Login as admin
            await LoginAsUser();
            
            // Generate unique bio text to verify save/display
            string testBio = $"Test bio created at {DateTime.Now:yyyy-MM-dd HH:mm:ss} - I love playing football and tennis!";

            // Act - Navigate to Account Settings
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open user menu user photo" }).ClickAsync();
            await Task.Delay(500); // Wait for dropdown
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Account Settings" }).ClickAsync();
            
            // Wait for Account Settings page to load
            await Page.WaitForURLAsync("**/Account/Manage", new PageWaitForURLOptions { Timeout = 10000 });
            await Task.Delay(1000); // Wait for page to fully render

            // Act - Fill in the Biography field
            ILocator bioTextbox = Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Biography" });
            await Expect(bioTextbox).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
            
            // Clear existing bio and enter new one
            await bioTextbox.ClickAsync();
            await bioTextbox.FillAsync(testBio);

            // Act - Click Save button
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Save" }).ClickAsync();
            
            // Wait for save to complete (look for success message or page reload)
            await Task.Delay(2000);

            // Assert - Bio field still contains the saved text
            await Expect(bioTextbox).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
            string savedBioValue = await bioTextbox.InputValueAsync();
            Assert.Equal(testBio, savedBioValue);

            // Assert - Navigate to user's profile to verify bio is displayed
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open user menu user photo" }).ClickAsync();
            await Task.Delay(500);
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "My Profile" }).ClickAsync();
            
            // Wait for profile page to load
            await Task.Delay(2000);
            
            // Assert - Bio is visible on the profile page
            await Expect(Page.GetByText(testBio)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
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

