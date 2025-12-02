/*using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace MatchBy.UnitTests.Playwright;

public class RegisterPageTests : PageTest
{
    private const string BaseUrl = "http://localhost:5029";
    private const string RegisterUrl = BaseUrl + "/Account/Register";
    private const string Username = "joao3";

    [Fact]
    public async Task RegisterPage_ShouldLoadSuccessfully()
    {
        try
        {
            await Page.GotoAsync(RegisterUrl);
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Email" })
                .FillAsync(Username + "@" + Username + ".com");
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Username" })
                .FillAsync(Username);
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Display Name" })
                .FillAsync(Username);
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Password", Exact = true })
                .FillAsync(Username + "aA123@1");
            await Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Confirm Password" })
                .FillAsync(Username + "aA123@1");
            await Page.GetByRole(AriaRole.Checkbox, new PageGetByRoleOptions { Name = "I accept the Terms and" })
                .CheckAsync();
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create account" }).ClickAsync();
            await Expect(Page.GetByText("We sent the link to")).ToBeVisibleAsync();
            await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Return to home" }).ClickAsync();
        }
        finally
        {
            await Page.CloseAsync();
        }
    }
}*/