using System.Net;
using WebApp.Models;

namespace WebApp.Tests;

// Runs the real app (TestWebAppFactory) and signs in through the actual login form
public class SignInTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public SignInTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    [Trait("Category", "Positive")]
    public async Task Demo_user_can_sign_in_and_is_greeted_by_name()
    {
        var browser = _factory.CreateBrowser();

        var response = await TestWebAppFactory.SignInAsync(browser, "ANNA", UserSeeder.DemoPassword);   // username isn't case-sensitive

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
        var home = await browser.GetStringAsync("/");
        Assert.Contains("Welcome back, Anna!", home);
    }

    [Fact]
    [Trait("Category", "Positive")]
    public async Task Signed_in_user_can_save_a_quest()
    {
        var browser = _factory.CreateBrowser();
        await TestWebAppFactory.SignInAsync(browser, "beatrice", UserSeeder.DemoPassword);
        var token = await TestWebAppFactory.GetAntiForgeryTokenAsync(browser, "/Quests/Quests?section=explore");

        var response = await browser.PostAsync("/Quests/ToggleSave", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["questId"] = "1"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Quests/Quests", response.Headers.Location?.OriginalString);
    }

    [Fact]
    [Trait("Category", "Negative")]
    public async Task Wrong_password_is_rejected_and_user_stays_signed_out()
    {
        var browser = _factory.CreateBrowser();

        var response = await TestWebAppFactory.SignInAsync(browser, "anna", "not-the-password");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);   // the form is shown again, no redirect
        Assert.Contains("Wrong username or password.", await response.Content.ReadAsStringAsync());
        Assert.Contains("Welcome!", await browser.GetStringAsync("/"));
    }

    [Fact]
    [Trait("Category", "Negative")]
    public async Task Unknown_username_gets_the_same_message_as_a_wrong_password()
    {
        var browser = _factory.CreateBrowser();

        var response = await TestWebAppFactory.SignInAsync(browser, "no-such-user", UserSeeder.DemoPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Same wording as a wrong password, so the form doesn't reveal which usernames exist
        Assert.Contains("Wrong username or password.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    [Trait("Category", "Negative")]
    public async Task Saving_a_quest_while_signed_out_redirects_to_sign_in_and_back()
    {
        var browser = _factory.CreateBrowser();
        var token = await TestWebAppFactory.GetAntiForgeryTokenAsync(browser, "/Quests/Quests?section=explore");
        var request = new HttpRequestMessage(HttpMethod.Post, "/Quests/ToggleSave")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["questId"] = "1"
            })
        };
        request.Headers.Referrer = new Uri(browser.BaseAddress!, "/Quests/Quests?section=explore");

        var response = await browser.SendAsync(request);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.OriginalString ?? string.Empty;
        Assert.StartsWith("/Account/Login", new Uri(browser.BaseAddress!, location).AbsolutePath);
        Assert.Contains("ReturnUrl=%2FQuests%2FQuests%3Fsection%3Dexplore", location);   // back to the page after sign-in
    }
}
