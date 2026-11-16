using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Models;

namespace WebApp.Tests;

public class AdminSetupTests
{
    [Fact]
    public async Task First_admin_setup_is_local_only_and_disappears_after_creation()
    {
        using var factory = new TestWebAppFactory();
        using var nonLocal = factory.CreateBrowser();
        Assert.Equal(HttpStatusCode.NotFound, (await nonLocal.GetAsync("/Account/SetupAdmin")).StatusCode);
        using var localFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddTransient<IStartupFilter, LoopbackRequests>()));
        using var local = localFactory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = true });
        var token = await TestWebAppFactory.GetAntiForgeryTokenAsync(local, "/Account/SetupAdmin");
        var response = await local.PostAsync("/Account/SetupAdmin", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token, ["Password"] = "local-test-password", ["ConfirmPassword"] = "local-test-password"
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Users", response.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await local.GetAsync("/Users")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await local.GetAsync("/Account/SetupAdmin")).StatusCode);
        using var scope = localFactory.Services.CreateScope();
        var admin = await scope.ServiceProvider.GetRequiredService<SheetsDbContext>().Users.SingleAsync(u => u.Username == "admin");
        Assert.True(admin.IsAdmin);
        Assert.NotEqual("local-test-password", admin.PasswordHash);
    }

    [Fact]
    public async Task Setup_requires_matching_valid_passwords()
    {
        using var factory = new TestWebAppFactory();
        using var localFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddTransient<IStartupFilter, LoopbackRequests>()));
        using var local = localFactory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = true });
        var token = await TestWebAppFactory.GetAntiForgeryTokenAsync(local, "/Account/SetupAdmin");
        var response = await local.PostAsync("/Account/SetupAdmin", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token, ["Password"] = "short", ["ConfirmPassword"] = "different"
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = localFactory.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<SheetsDbContext>().Users.AnyAsync(u => u.IsAdmin));
    }

    private sealed class LoopbackRequests : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Loopback;
                await nextMiddleware();
            });
            next(app);
        };
    }
}
