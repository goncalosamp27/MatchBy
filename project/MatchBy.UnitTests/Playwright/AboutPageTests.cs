using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

public class AboutPageTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";

    [Fact]
    public async Task AboutPage_DisplaysProjectInformation()
    {
        try
        {
            // Navigate to home page
            await Page.GotoAsync(BaseUrl);
            await Task.Delay(1000);

            // Click on About link
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "About" }).ClickAsync();
            await Task.Delay(1000);

            // Verify About page content is visible
            // Check for typical About page content elements
            ILocator aboutContent = Page.Locator("h1, h2").Filter(new LocatorFilterOptions 
            { 
                HasTextRegex = new System.Text.RegularExpressions.Regex("About", System.Text.RegularExpressions.RegexOptions.IgnoreCase) 
            }).First;
            
            await Expect(aboutContent).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });

            // Verify URL contains /about
            string currentUrl = Page.Url;
            Assert.Contains("/about", currentUrl.ToLower(), StringComparison.OrdinalIgnoreCase);
        }


        catch (Exception ex)
        {
            Console.WriteLine($"Test failed with exception: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}
