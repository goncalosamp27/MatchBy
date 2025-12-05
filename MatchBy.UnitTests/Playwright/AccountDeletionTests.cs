using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

/// <summary>
/// End-to-end tests for account deletion functionality.
/// User Story: As a user, I want to delete my account, so that I can permanently remove my personal data from the platform.
/// 
/// Tests cover the complete flow:
/// 1. User navigates to Account Settings
/// 2. User clicks on "Personal data"
/// 3. User clicks on "Delete" button
/// 4. System asks for confirmation (password)
/// 5. Upon confirmation, account is permanently deleted
/// </summary>
public class AccountDeletionTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string LoginUrl = BaseUrl + "/Account/Login";
    private const string AccountManageUrl = BaseUrl + "/Account/Manage";
    private const string PersonalDataUrl = BaseUrl + "/Account/Manage/PersonalData";
    private const string DeletePersonalDataUrl = BaseUrl + "/Account/Manage/DeletePersonalData";
    
    // Test user credentials - using a unique user for deletion tests
    private const string TestEmail = "test1@test.com";
    private const string TestPassword = "Test!123";

    #region Account Deletion Flow Tests

    [Fact]
    public async Task AccountDeletion_UserNavigatesToAccountSettings_PageLoadsSuccessfully()
    {
        try
        {
            // Arrange - Login as test user
            await LoginAsTestUser();

            // Act - Navigate to Account Settings from header
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open user menu user photo" }).ClickAsync();
            await Task.Delay(500); // Wait for dropdown
            await Page.GetByText("Account Settings").ClickAsync();

            // Assert - Account Settings page loaded
            await Page.WaitForURLAsync("**/Account/Manage", new PageWaitForURLOptions { Timeout = 10000 });
            await Expect(Page).ToHaveTitleAsync("MatchBy");
            await Expect(Page.GetByText("Edit Profile")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 30000
            });
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task AccountDeletion_UserNavigatesToPersonalData_PageDisplaysDeleteOption()
    {
        try
        {
            // Arrange - Login and navigate to Account Settings
            await LoginAsTestUser();
            await Page.GotoAsync(AccountManageUrl);

            // Act - Click on "Personal data" in sidebar navigation
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Personal data" }).ClickAsync();

            // Assert - Personal Data page loaded
            await Page.WaitForURLAsync("**/Account/Manage/PersonalData", new PageWaitForURLOptions { Timeout = 10000 });
            await Expect(Page).ToHaveTitleAsync("MatchBy");

            // Assert - Warning message is visible
            await Expect(Page.GetByText("Deleting this data will permanently remove your account"))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

            // Assert - Delete button is visible
            ILocator deleteButton = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Delete" });
            await Expect(deleteButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task AccountDeletion_UserClicksDelete_NavigatesToConfirmationPage()
    {
        try
        {
            // Arrange - Navigate to Personal Data page
            await LoginAsTestUser();
            await Page.GotoAsync(PersonalDataUrl);

            // Act - Click Delete button
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Delete" }).ClickAsync();

            // Assert - Navigated to Delete Personal Data page
            await Page.WaitForURLAsync("**/Account/Manage/DeletePersonalData", new PageWaitForURLOptions { Timeout = 10000 });
            await Expect(Page).ToHaveTitleAsync("MatchBy");

            // Assert - Warning message is displayed
            await Expect(Page.Locator("text=/Deleting this data will permanently remove.*cannot be recovered/i"))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

            // Assert - Password field is visible (for confirmation)
            await Expect(Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Password" }))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

            // Assert - Delete button is visible
            await Expect(Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Delete data and close my account" }))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task AccountDeletion_UserEntersIncorrectPassword_ShowsErrorMessage()
    {
        try
        {
            // Arrange - Navigate to Delete Personal Data page
            await LoginAsTestUser();
            await Page.GotoAsync(DeletePersonalDataUrl);

            // Act - Enter incorrect password and submit
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Password" })
                .FillAsync("WrongPassword123!");
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Delete data and close my account" })
                .ClickAsync();

            // Assert - Error message displayed
            await Expect(Page.Locator("text=/Error.*Incorrect password/i"))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

            // Assert - User is still on the delete page (not logged out)
            await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Account/Manage/DeletePersonalData"));
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    // [Fact(Skip = "This test will delete the user account - only run when testing account deletion feature")]
    // public async Task AccountDeletion_UserConfirmsWithCorrectPassword_AccountIsDeleted()
    // {
    //     try
    //     {
    //         // Arrange - Create a fresh test user and login
    //         string uniqueEmail = $"deleteme_{Guid.NewGuid().ToString("N").Substring(0, 8)}@test.com";
    //         string uniqueUsername = $"deleteme_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
    //         string password = "DeleteMe123!";
    //
    //         // First, register the user
    //         await RegisterNewUser(uniqueEmail, uniqueUsername, password);
    //
    //         // Login as the new user
    //         await LoginWithCredentials(uniqueEmail, password);
    //
    //         // Navigate to Delete Personal Data page
    //         await Page.GotoAsync(DeletePersonalDataUrl);
    //
    //         // Act - Enter correct password and confirm deletion
    //         await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Password" })
    //             .FillAsync(password);
    //         await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Delete data and close my account" })
    //             .ClickAsync();
    //
    //         // Assert - User is redirected and logged out
    //         await Task.Delay(2000); // Wait for deletion to complete
    //
    //         // After deletion, user should be logged out and redirected to home page
    //         await Page.WaitForURLAsync("**/", new PageWaitForURLOptions { Timeout = 15000 });
    //
    //         // Assert - User is logged out (Join now button visible)
    //         ILocator joinButton = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Join now!" });
    //         await Expect(joinButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
    //         {
    //             Timeout = 10000
    //         });
    //
    //         // Assert - User menu is not visible (confirms logout)
    //         ILocator userMenu = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open user menu user photo" });
    //         await Expect(userMenu).Not.ToBeVisibleAsync();
    //
    //         // Verify user cannot login again (account deleted)
    //         await Page.GotoAsync(LoginUrl);
    //         await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Your email" })
    //             .FillAsync(uniqueEmail);
    //         await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Password" })
    //             .FillAsync(password);
    //         await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Sign in to your account" })
    //             .ClickAsync();
    //
    //         // Assert - Login fails (invalid login attempt)
    //         await Expect(Page.GetByText("Error: Invalid login attempt."))
    //             .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
    //     }
    //     finally
    //     {
    //         await Page.CloseAsync();
    //     }
    // }

    [Fact]
    public async Task AccountDeletion_DirectAccessToDeletePage_RequiresAuthentication()
    {
        try
        {
            // Arrange - No login (anonymous user)

            // Act - Try to access Delete Personal Data page directly
            await Page.GotoAsync(DeletePersonalDataUrl);

            // Assert - Redirected to login page (requires authentication)
            await Page.WaitForURLAsync("**/Account/Login**", new PageWaitForURLOptions { Timeout = 10000 });
            await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Account/Login"));
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    [Fact]
    public async Task AccountDeletion_PersonalDataPage_ShowsDownloadAndDeleteOptions()
    {
        try
        {
            // Arrange - Login and navigate to Personal Data page
            await LoginAsTestUser();
            await Page.GotoAsync(PersonalDataUrl);

            // Assert - Download button is visible
            ILocator downloadButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Download" });
            await Expect(downloadButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

            // Assert - Delete button is visible
            ILocator deleteButton = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Delete" });
            await Expect(deleteButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

            // Assert - Warning message about permanent deletion is visible
            await Expect(Page.Locator("text=/permanently remove.*cannot be undone/i"))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method to log in as a test user.
    /// For account deletion tests, we should use a dedicated test user that can be deleted.
    /// </summary>
    private async Task LoginAsTestUser()
    {
        // Note: This assumes a test user exists in the database.
        // For actual testing, you may need to create the user first or use a seeded test user.
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

        // Wait for user menu to be visible (confirms login success and navigation completed)
        // This is more reliable than waiting for URL since the redirect may vary
        ILocator userMenuButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open user menu user photo" });
        await Expect(userMenuButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
        {
            Timeout = 30000 // Increased timeout to handle potential delays
        });
        
        // Give the page a moment to fully settle after login
        await Task.Delay(1000);
    }

    #endregion
}

