using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

public class HomePageTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";

    [Fact]
    public async Task HomePage_ShouldLoadSuccessfully()
    {
        try
        {
            // Act - Navigate to home page
            await Page.GotoAsync(BaseUrl);

            // Assert - Check page title
            string title = await Page.TitleAsync();
            Assert.Contains("MatchBy", title);

            // Assert - Check main heading is visible
            ILocator heading = Page.Locator("h1").First;
            await Assertions.Expect(heading).ToBeVisibleAsync();

            // Assert - Check that the page contains MatchBy text
            string? bodyText = await Page.TextContentAsync("body");
            Assert.NotNull(bodyText);
            Assert.Contains("MatchBy", bodyText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await Page.CloseAsync();
        }
    }

    /*[Fact]
    public async Task HomePage_ShouldHaveJoinNowButton_WhenNotAuthenticated()
    {
        // Arrange
        IBrowserContext context = await fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true // Ignore self-signed certificate errors for localhost
        });
        IPage page = await context.NewPageAsync();
        const string baseUrl = "http://localhost:5029";

        try
        {
            // Act
            await page.GotoAsync(baseUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });

            // Assert - Check for "Join now!" button or link
            ILocator joinButton = page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Join now!" }).Or(page.Locator("a:has-text('Join now!')"));
            await Assertions.Expect(joinButton.First).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
            await context.CloseAsync();
        }
    }*/
}

