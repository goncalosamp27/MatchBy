using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

public class SearchMatchesTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string LoginUrl = BaseUrl + "/Account/Login";
    private const string Test1Email = "test1@test.com";
    private const string Test1Password = "Test!123";
    private const string Test2Email = "test2@test.com";
    private const string Test2Password = "Test!123";
    
    private static readonly string[] FootballOption = ["Football"];
    private static readonly string[] BasketballOption = ["Basketball"];

    [Fact]
    public async Task SearchMatches_DisplaysAllAvailableMatches()
    {
        string footballMatchAddress = $"Football Match {DateTime.Now.ToString("HHmmss", CultureInfo.InvariantCulture)}";
        string footballMatchDescription = "Join our football game!";
        
        string basketballMatchAddress = $"Basketball Match {DateTime.Now.ToString("HHmmss", CultureInfo.InvariantCulture)}";
        string basketballMatchDescription = "Come play basketball!";

        try
        {
            // === Part 1: Test1 creates a Football match ===
            await LoginWithCredentials(Test1Email, Test1Password);

            // Navigate to Matches page
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Matches" }).First.ClickAsync();
            await Task.Delay(1000);

            // Create Football match
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "+ Create Match" }).ClickAsync();
            await Task.Delay(1000);

            // Fill in match details for Football
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Type your match address" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Type your match address" }).FillAsync(footballMatchAddress);
            
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Type a description for your" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Type a description for your" }).FillAsync(footballMatchDescription);
            
            // Select Football sport (first combobox is Sport)
            await Page.GetByRole(AriaRole.Combobox).First.SelectOptionAsync(FootballOption);
            
            // Create the match
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create Match" }).ClickAsync();
            await Task.Delay(2000);

            // === Part 2: Test1 creates a Basketball match ===
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "+ Create Match" }).ClickAsync();
            await Task.Delay(1000);

            // Fill in match details for Basketball
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Type your match address" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Type your match address" }).FillAsync(basketballMatchAddress);
            
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Type a description for your" }).ClickAsync();
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Type a description for your" }).FillAsync(basketballMatchDescription);
            
            // Select Basketball sport (first combobox is Sport)
            await Page.GetByRole(AriaRole.Combobox).First.SelectOptionAsync(BasketballOption);
            
            // Create the match
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create Match" }).ClickAsync();
            await Task.Delay(2000);

            // Logout Test1
            await LogoutUser();

            // === Part 3: Test2 searches for matches ===
            await LoginWithCredentials(Test2Email, Test2Password);

            // Navigate to Matches page
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Matches" }).First.ClickAsync();
            await Task.Delay(1000);

            // Click on "Search Matches" tab
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Search Matches" }).ClickAsync();
            await Task.Delay(1000);

            // Verify both matches are visible in the search results (system displays all available matches)
            ILocator footballMatch = Page.GetByText(footballMatchAddress).First;
            await Expect(footballMatch).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

            ILocator basketballMatch = Page.GetByText(basketballMatchAddress).First;
            await Expect(basketballMatch).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

            // Logout Test2
            await LogoutUser();

            // === Part 4: Test1 deletes the matches ===
            await LoginWithCredentials(Test1Email, Test1Password);

            // Navigate to Matches page
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Matches" }).First.ClickAsync();
            await Task.Delay(1000);

            // Switch to My Matches tab
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "My Matches" }).ClickAsync();
            await Task.Delay(1000);

            // Delete Football match
            ILocator footballMatchCard = Page.GetByText(footballMatchAddress).First;
            await footballMatchCard.ClickAsync();
            await Task.Delay(1000);
            
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Cancel Match" }).ClickAsync();
            await Task.Delay(2000);

            // Navigate back to matches
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Matches" }).First.ClickAsync();
            await Task.Delay(1000);

            // Switch to My Matches tab again
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "My Matches" }).ClickAsync();
            await Task.Delay(1000);

            // Delete Basketball match
            ILocator basketballMatchCard = Page.GetByText(basketballMatchAddress).First;
            await basketballMatchCard.ClickAsync();
            await Task.Delay(1000);
            
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Cancel Match" }).ClickAsync();
            await Task.Delay(2000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed with exception: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private async Task LoginWithCredentials(string email, string password)
    {
        await Page.GotoAsync(LoginUrl);
        
        // Fill in email
        await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Email" })
            .FillAsync(email);
        
        // Fill in password
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

    private async Task LogoutUser()
    {
        // Click user menu
        ILocator userMenuButton = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open user menu user photo" });
        await userMenuButton.ClickAsync();
        await Task.Delay(500);

        // Click logout button
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Logout" }).ClickAsync();
        await Task.Delay(2000);
    }
}
