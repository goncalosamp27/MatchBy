using Microsoft.Playwright;
using Xunit;

namespace MatchBy.UnitTests.Playwright;

public class PlaywrightFixture : IAsyncLifetime
{
    public IBrowser Browser { get; private set; } = null!;
    private IPlaywright _playwright = null!;

    public async Task InitializeAsync()
    {
        // Install Playwright browsers if not already installed
        // This will only install if needed
        Program.Main(["install", "chromium"]);

        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        await Browser.CloseAsync();

        _playwright?.Dispose();
    }
}

