using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

public class OAuthTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string LoginUrl = BaseUrl + "/Account/Login";

    [Fact]
    public async Task GoogleSignIn_RedirectsToGoogleOAuth()
    {
        try
        {
            // Navigate to login page
            await Page.GotoAsync(LoginUrl);
            await Task.Delay(1000);

            // Find the Google sign-in button
            ILocator googleSignInButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Log in using your Google" });
            
            // Verify button exists
            await Expect(googleSignInButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });

            // Click and wait for navigation to Google OAuth
            await googleSignInButton.ClickAsync();
            await Page.WaitForURLAsync("**/accounts.google.com/**", new PageWaitForURLOptions { Timeout = 10000 });

            // Verify we've been redirected to Google OAuth
            string currentUrl = Page.Url;
            Assert.Contains("accounts.google.com", currentUrl);
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed with exception: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    [Fact]
    public async Task GitHubSignIn_RedirectsToGitHubOAuth()
    {
        try
        {
            // Navigate to login page
            await Page.GotoAsync(LoginUrl);
            await Task.Delay(1000);

            // Find the GitHub sign-in button
            ILocator githubSignInButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Log in using your GitHub" });
            
            // Verify button exists
            await Expect(githubSignInButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });

            // Click and wait for navigation to GitHub OAuth
            await githubSignInButton.ClickAsync();
            await Page.WaitForURLAsync("**/github.com/**", new PageWaitForURLOptions { Timeout = 10000 });

            // Verify we've been redirected to GitHub OAuth
            string currentUrl = Page.Url;
            Assert.Contains("github.com", currentUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed with exception: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    [Fact]
    public async Task DiscordSignIn_RedirectsToDiscordOAuth()
    {
        try
        {
            // Navigate to login page
            await Page.GotoAsync(LoginUrl);
            await Task.Delay(1000);

            // Find the Discord sign-in button
            ILocator discordSignInButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Log in using your Discord" });
            
            // Verify button exists
            await Expect(discordSignInButton).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });

            // Click and wait for navigation to Discord OAuth
            await discordSignInButton.ClickAsync();
            await Page.WaitForURLAsync("**/discord.com/**", new PageWaitForURLOptions { Timeout = 10000 });

            // Verify we've been redirected to Discord OAuth
            string currentUrl = Page.Url;
            Assert.Contains("discord.com", currentUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed with exception: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}
