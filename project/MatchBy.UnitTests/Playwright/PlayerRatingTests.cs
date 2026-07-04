using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

public class PlayerRatingTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string LoginUrl = BaseUrl + "/Account/Login";
    private const string Test1Email = "test1@test.com";
    private const string Test1Password = "Test!123";
    private const string Test2Email = "test2@test.com";

    [Fact]
    public async Task ViewPlayerProfile_DisplaysAverageRating()
    {
        try
        {
            // Login as test1
            await LoginWithCredentials(Test1Email, Test1Password);

            // Click on Users link
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Users" }).ClickAsync();
            await Task.Delay(1000);

            // Click on test2's profile
            await Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = Test2Email }).ClickAsync();
            await Task.Delay(1000);

            // Verify a rating is displayed (should be a number between 0 and 5)
            // Look for ratings like "5.0", "4.5", "3.0", etc.
            ILocator ratingLocator = Page.Locator("text=/^[0-5](\\.[0-9])?$/").First;
            await Expect(ratingLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });

            // Verify the rating is within valid range (0-5)
            string ratingText = await ratingLocator.TextContentAsync() ?? "0";
            double rating = double.Parse(ratingText, System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(rating >= 0 && rating <= 5, $"Rating should be between 0 and 5, but was {rating}");
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
