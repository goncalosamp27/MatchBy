using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

/// <summary>
/// End-to-end tests for the Home page (/).
/// Tests cover anonymous user scenarios - title, hero text, navigation.
/// Based on Home.razor component.
/// </summary>
public class HomePageTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string HomeUrl = BaseUrl + "/";

    [Fact]
    public async Task HomePage_LoadsSuccessfully_VerifyTitleAndHeroText()
    {
        // Arrange & Act
        await Page.GotoAsync(HomeUrl);

        // Assert - Page title in browser tab
        await Expect(Page).ToHaveTitleAsync("MatchBy");

        // Assert - Hero description paragraph is visible
        await Expect(Page.GetByText("MatchBy is the best way to connect with other players and organize matches")).ToBeVisibleAsync();

        // Assert - Hero heading is visible (one of the rotating messages)
        // Messages include: "Are you ready to play your favorite sports?", 
        // "Find teammates near you and play!", etc.
        ILocator heroHeading = Page.Locator("h1.text-4xl, h1.text-5xl, h1.text-6xl").First;
        await Expect(heroHeading).ToBeVisibleAsync();
        
        // Verify heading has actual text content
        String headingText = await heroHeading.TextContentAsync();
        Assert.NotNull(headingText);
        Assert.NotEmpty(headingText!.Trim());
        
        // Verify it's one of the expected messages (at least contains some key words)
        Boolean hasExpectedContent = headingText.Contains("ready", StringComparison.OrdinalIgnoreCase) ||
                                 headingText.Contains("match", StringComparison.OrdinalIgnoreCase) ||
                                 headingText.Contains("play", StringComparison.OrdinalIgnoreCase) ||
                                 headingText.Contains("Find", StringComparison.OrdinalIgnoreCase);
        Assert.True(hasExpectedContent, $"Hero heading '{headingText}' should contain expected keywords");
    }
    
    [Fact]
    public async Task HomePage_AnonymousUser_ShowsJoinNowButton()
    {
        // Arrange & Act
        await Page.GotoAsync(HomeUrl);

        // Assert - "Join now!" button visible for anonymous users
        ILocator joinButton = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Join now!" });
        await Expect(joinButton).ToBeVisibleAsync();
    }

    [Fact]
    public async Task HomePage_AnonymousUser_JoinNowNavigatesToRegister()
    {
        // Arrange
        await Page.GotoAsync(HomeUrl);

        // Act - Click "Join now!" button
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Join now!" }).ClickAsync();

        // Assert - Navigated to registration page
        await Page.WaitForURLAsync("**/Account/Register");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Account/Register"));
    }

    [Fact]
    public async Task HomePage_LearnMoreNavigatesToAbout()
    {
        // Arrange
        await Page.GotoAsync(HomeUrl);

        // Act - Click "Learn more" button
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Learn more" }).ClickAsync();

        // Assert - Navigated to About page
        await Page.WaitForURLAsync("**/About");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/About"));
    }

    [Fact]
    public async Task HomePage_CreateMatchSection_Visible()
    {
        // Arrange & Act
        await Page.GotoAsync(HomeUrl);

        // Assert - "Create your Match!" heading visible
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Create your Match!" }))
            .ToBeVisibleAsync();

        // Assert - Description text visible
        await Expect(Page.GetByText("Easily create, organize, share, and manage matches."))
            .ToBeVisibleAsync();
        
        // Assert - "Let's go!" button visible
        await Expect(Page.GetByText("Let's go!")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task HomePage_LetsGoButton_NavigatesToCreateMatch()
    {
        // Arrange
        await Page.GotoAsync(HomeUrl);

        // Act - Click "Let's go!" button
        await Page.GetByText("Let's go!").ClickAsync();

        // Assert - Verify navigation to create match page
        // For anonymous users, may redirect to login first, so we check URL changed
        await Task.Delay(500); // Allow navigation to complete
        String currentUrl = Page.Url;
        
        // Should navigate away from home (to /matches/create or login)
        Assert.NotEqual(HomeUrl, currentUrl);
        
        // Check if ended up at create match or login page
        Boolean isCreateMatchOrLogin = currentUrl.Contains("/matches/create", StringComparison.OrdinalIgnoreCase) ||
                                    currentUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase);
        Assert.True(isCreateMatchOrLogin, $"Expected navigation to create match or login, got: {currentUrl}");
    }
}
