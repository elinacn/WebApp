using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Models;

namespace WebApp.Tests;

public class PasswordChangeTests
{
    [Theory]
    [Trait("Category", "Negative")]
    [InlineData(null, "Enter your current password to change it.")]
    [InlineData("wrong-password", "Your current password is incorrect.")]
    public async Task Missing_or_wrong_current_password_rejects_the_entire_edit(string? currentPassword, string error)
    {
        using var factory = new TestWebAppFactory();
        using var browser = factory.CreateBrowser();
        await TestWebAppFactory.SignInAsync(browser, "anna", UserSeeder.DemoPassword);

        var response = await EditAsync(factory, browser, currentPassword, "replacement123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(error, await response.Content.ReadAsStringAsync());
        using var scope = factory.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<SheetsDbContext>().Users.SingleAsync(u => u.Username == "anna");
        Assert.Equal("Anna", user.Name);
        using var oldLogin = factory.CreateBrowser();
        Assert.Equal(HttpStatusCode.Redirect,
            (await TestWebAppFactory.SignInAsync(oldLogin, "anna", UserSeeder.DemoPassword)).StatusCode);
        using var newLogin = factory.CreateBrowser();
        Assert.Contains("Wrong username or password.", await
            (await TestWebAppFactory.SignInAsync(newLogin, "anna", "replacement123")).Content.ReadAsStringAsync());
    }

    [Fact]
    [Trait("Category", "Positive")]
    public async Task Correct_current_password_allows_changing_password()
    {
        using var factory = new TestWebAppFactory();
        using var browser = factory.CreateBrowser();
        await TestWebAppFactory.SignInAsync(browser, "anna", UserSeeder.DemoPassword);

        var response = await EditAsync(factory, browser, UserSeeder.DemoPassword, "replacement123");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var newLogin = factory.CreateBrowser();
        Assert.Equal(HttpStatusCode.Redirect,
            (await TestWebAppFactory.SignInAsync(newLogin, "anna", "replacement123")).StatusCode);
        using var oldLogin = factory.CreateBrowser();
        Assert.Contains("Wrong username or password.", await
            (await TestWebAppFactory.SignInAsync(oldLogin, "anna", UserSeeder.DemoPassword)).Content.ReadAsStringAsync());
    }

    [Fact]
    [Trait("Category", "Positive")]
    public async Task Profile_edit_without_new_password_does_not_require_current_password()
    {
        using var factory = new TestWebAppFactory();
        using var browser = factory.CreateBrowser();
        await TestWebAppFactory.SignInAsync(browser, "anna", UserSeeder.DemoPassword);

        var response = await EditAsync(factory, browser, null, null);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<SheetsDbContext>().Users.SingleAsync(u => u.Username == "anna");
        Assert.Equal("Updated Anna", user.Name);
        using var login = factory.CreateBrowser();
        Assert.Equal(HttpStatusCode.Redirect,
            (await TestWebAppFactory.SignInAsync(login, "anna", UserSeeder.DemoPassword)).StatusCode);
    }

    private static async Task<HttpResponseMessage> EditAsync(
        TestWebAppFactory factory, HttpClient browser, string? currentPassword, string? newPassword)
    {
        using var scope = factory.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<SheetsDbContext>().Users.AsNoTracking()
            .SingleAsync(u => u.Username == "anna");
        var token = await TestWebAppFactory.GetAntiForgeryTokenAsync(browser, $"/Users/Edit/{user.Id}");
        return await browser.PostAsync($"/Users/Edit/{user.Id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = user.Id.ToString(),
            ["Username"] = user.Username,
            ["Name"] = "Updated Anna",
            ["StudyProgram"] = user.StudyProgram,
            ["CreatedAt"] = user.CreatedAt.ToString("yyyy-MM-dd"),
            ["currentPassword"] = currentPassword ?? string.Empty,
            ["newPassword"] = newPassword ?? string.Empty
        }));
    }
}
